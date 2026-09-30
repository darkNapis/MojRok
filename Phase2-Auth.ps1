# =============================================================
# MojRok — Phase 2: Authentication
# Run from: C:\Users\APIS\OneDrive\Desktop\MojRok
# Prerequisite: Phase 1 must be complete (MojRok.sln must exist)
# =============================================================

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step { param($msg) Write-Host "`n>>> $msg" -ForegroundColor Cyan }
function Write-Done { param($msg) Write-Host "    [OK] $msg" -ForegroundColor Green }
function Write-Warn { param($msg) Write-Host "    [WARN] $msg" -ForegroundColor Yellow }
function Write-Fatal { param($msg) Write-Host "`n[FATAL] $msg" -ForegroundColor Red; exit 1 }

if (-not (Test-Path 'MojRok.sln')) {
    Write-Fatal "MojRok.sln not found. Run from C:\Users\APIS\OneDrive\Desktop\MojRok"
}

function Write-File {
    param([string]$Path, [string]$Content)
    $dir = Split-Path -Path $Path -Parent
    if ($dir -and -not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    Set-Content -Path $Path -Value $Content -Encoding UTF8
    Write-Done "  $Path"
}

Write-Host "`n=== MojRok Phase 2 — Authentication ===" -ForegroundColor Yellow

# -----------------------------------------------------------
# STEP 1 — Update .csproj files
# -----------------------------------------------------------
Write-Step 'STEP 1 — Updating .csproj files'

# Infrastructure: add BCrypt + JWT token generator
Set-Content -Path 'src\MojRok.Infrastructure\MojRok.Infrastructure.csproj' -Encoding UTF8 -Value @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.7">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
	  <Publish>true</Publish>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\MojRok.Application\MojRok.Application.csproj" />
    <ProjectReference Include="..\MojRok.Domain\MojRok.Domain.csproj" />
  </ItemGroup>
</Project>
'@
Write-Done 'MojRok.Infrastructure.csproj'

# Tests: add InMemory provider + Infrastructure reference for auth tests
Set-Content -Path 'tests\MojRok.Tests\MojRok.Tests.csproj' -Encoding UTF8 -Value @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.7" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Memory" Version="9.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk"    Version="17.12.0" />
    <PackageReference Include="xunit"                     Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
	  <Publish>true</Publish>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.3">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
	  <Publish>true</Publish>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\MojRok.Domain\MojRok.Domain.csproj" />
    <ProjectReference Include="..\..\src\MojRok.Application\MojRok.Application.csproj" />
    <ProjectReference Include="..\..\src\MojRok.Infrastructure\MojRok.Infrastructure.csproj" />
  </ItemGroup>
</Project>
'@
Write-Done 'MojRok.Tests.csproj'

# -----------------------------------------------------------
# STEP 2 — Application layer: Abstractions
# -----------------------------------------------------------
Write-Step 'STEP 2 — Application abstractions'

Write-File 'src\MojRok.Application\Abstractions\IJwtService.cs' @'
using MojRok.Domain.Entities;

namespace MojRok.Application.Abstractions;

/// <summary>
/// Generates JWT tokens. Implemented in Infrastructure.
/// Returns the token string and its exact expiry together
/// so the response matches the actual token lifetime.
/// </summary>
public interface IJwtService
{
    (string Token, DateTime ExpiresAt) GenerateToken(AppUser user);
}
'@

Write-File 'src\MojRok.Application\Abstractions\IPasswordHasher.cs' @'
namespace MojRok.Application.Abstractions;

/// <summary>
/// Hashes and verifies passwords. Implemented in Infrastructure using BCrypt.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
'@

# -----------------------------------------------------------
# STEP 3 — Application layer: Auth
# -----------------------------------------------------------
Write-Step 'STEP 3 — Application Auth (DTOs + AuthService)'

Write-File 'src\MojRok.Application\Auth\DTOs\RegisterRequest.cs' @'
using System.ComponentModel.DataAnnotations;

namespace MojRok.Application.Auth.DTOs;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;
}
'@

Write-File 'src\MojRok.Application\Auth\DTOs\LoginRequest.cs' @'
using System.ComponentModel.DataAnnotations;

namespace MojRok.Application.Auth.DTOs;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
'@

Write-File 'src\MojRok.Application\Auth\DTOs\AuthResponse.cs' @'
namespace MojRok.Application.Auth.DTOs;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
'@

Write-File 'src\MojRok.Application\Exceptions\AuthException.cs' @'
namespace MojRok.Application.Exceptions;

/// <summary>
/// Thrown when authentication fails (wrong credentials, inactive account).
/// Maps to HTTP 401 in the API layer.
/// </summary>
public sealed class AuthException : Exception
{
    public AuthException(string message) : base(message) { }
}
'@

Write-File 'src\MojRok.Application\Auth\AuthService.cs' @'
using Microsoft.EntityFrameworkCore;
using MojRok.Application.Abstractions;
using MojRok.Application.Auth.DTOs;
using MojRok.Application.Exceptions;
using MojRok.Domain.Entities;
using MojRok.Domain.Enums;

namespace MojRok.Application.Auth;

public class AuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public AuthService(
        IAppDbContext db,
        IPasswordHasher passwordHasher,
        IJwtService jwtService)
    {
        _db             = db;
        _passwordHasher = passwordHasher;
        _jwtService     = jwtService;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (emailExists)
            throw new InvalidOperationException("Email is already registered.");

        var user = new AppUser
        {
            Id           = Guid.NewGuid(),
            Email        = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName     = request.FullName.Trim(),
            Role         = UserRole.Citizen,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var (token, expiresAt) = _jwtService.GenerateToken(user);
        return new AuthResponse
        {
            Token     = token,
            ExpiresAt = expiresAt,
            FullName  = user.FullName,
            Role      = user.Role.ToString()
        };
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Intentionally identical error for "not found" and "wrong password"
        // so callers cannot enumerate registered emails.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new AuthException("Invalid email or password.");

        if (!user.IsActive)
            throw new AuthException("This account has been deactivated.");

        var (token, expiresAt) = _jwtService.GenerateToken(user);
        return new AuthResponse
        {
            Token     = token,
            ExpiresAt = expiresAt,
            FullName  = user.FullName,
            Role      = user.Role.ToString()
        };
    }
}
'@

# -----------------------------------------------------------
# STEP 4 — Infrastructure layer: Auth implementations
# -----------------------------------------------------------
Write-Step 'STEP 4 — Infrastructure Auth (JwtService + PasswordHasher)'

Write-File 'src\MojRok.Infrastructure\Auth\PasswordHasher.cs' @'
using MojRok.Application.Abstractions;

namespace MojRok.Infrastructure.Auth;

public class PasswordHasher : IPasswordHasher
{
    // Work factor 12 is the recommended minimum as of 2024.
    // Increase for higher security at the cost of hash time.
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
'@

Write-File 'src\MojRok.Infrastructure\Auth\JwtService.cs' @'
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MojRok.Application.Abstractions;
using MojRok.Domain.Entities;

namespace MojRok.Infrastructure.Auth;

public class JwtService : IJwtService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiresInMinutes;

    public JwtService(IConfiguration configuration)
    {
        _key             = configuration["Jwt:Key"]
                           ?? throw new InvalidOperationException(
                               "Jwt:Key is not configured. Use dotnet user-secrets.");
        _issuer          = configuration["Jwt:Issuer"]          ?? "MojRok";
        _audience        = configuration["Jwt:Audience"]        ?? "MojRok";
        _expiresInMinutes = int.TryParse(configuration["Jwt:ExpiresInMinutes"], out var m) ? m : 60;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(AppUser user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_expiresInMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,      user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email,    user.Email),
            new Claim(JwtRegisteredClaimNames.Jti,      Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role,                  user.Role.ToString()),
            new Claim("fullName",                       user.FullName)
        };

        var signingKey  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
'@

# -----------------------------------------------------------
# STEP 5 — Update InfrastructureServiceExtensions
# -----------------------------------------------------------
Write-Step 'STEP 5 — Update InfrastructureServiceExtensions'

Write-File 'src\MojRok.Infrastructure\Extensions\InfrastructureServiceExtensions.cs' @'
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MojRok.Application.Abstractions;
using MojRok.Infrastructure.Auth;
using MojRok.Infrastructure.Persistence;

namespace MojRok.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. " +
                "Use dotnet user-secrets for local development.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAppDbContext>(sp =>
            sp.GetRequiredService<AppDbContext>());

        // Auth services — implementations live in Infrastructure,
        // interfaces are defined in Application so the app layer stays DB-agnostic.
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();

        return services;
    }
}
'@

# -----------------------------------------------------------
# STEP 6 — API: AuthController
# -----------------------------------------------------------
Write-Step 'STEP 6 — API AuthController'

Write-File 'src\MojRok.API\Controllers\AuthController.cs' @'
using Microsoft.AspNetCore.Mvc;
using MojRok.Application.Auth;
using MojRok.Application.Auth.DTOs;
using MojRok.Application.Exceptions;

namespace MojRok.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Registers a new citizen account and returns a JWT.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _authService.RegisterAsync(request, ct);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Authenticates a citizen and returns a JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _authService.LoginAsync(request, ct);
            return Ok(response);
        }
        catch (AuthException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
'@

# -----------------------------------------------------------
# STEP 7 — API: Program.cs with JWT middleware
# -----------------------------------------------------------
Write-Step 'STEP 7 — API Program.cs'

Write-File 'src\MojRok.API\Program.cs' @'
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MojRok.Application.Auth;
using MojRok.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- Infrastructure (DbContext, IPasswordHasher, IJwtService) ---
builder.Services.AddInfrastructure(builder.Configuration);

// --- Application services ---
builder.Services.AddScoped<AuthService>();

// --- Controllers ---
builder.Services.AddControllers();

// --- JWT Authentication ---
// Key MUST be set via user-secrets or environment variable.
// It is never stored in appsettings.json.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key is not configured. " +
        "Run: dotnet user-secrets set \"Jwt:Key\" \"<your-key>\" --project src/MojRok.API");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// --- Swagger with JWT support ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MojRok API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Paste your JWT token here. Example: Bearer eyJhbGci..."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();   // must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();
'@

# -----------------------------------------------------------
# STEP 8 — appsettings.json: add Jwt section (no Key)
# -----------------------------------------------------------
Write-Step 'STEP 8 — appsettings.json'

Write-File 'src\MojRok.API\appsettings.json' @'
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Issuer": "MojRok",
    "Audience": "MojRok",
    "ExpiresInMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
'@

# appsettings.Development.json: connection string placeholder only.
# JWT Key stays in user-secrets — never here.
Write-File 'src\MojRok.API\appsettings.Development.json' @'
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mojrok_dev;Username=postgres;Password=<YOUR_LOCAL_PASSWORD>"
  }
}
'@

# -----------------------------------------------------------
# STEP 9 — Tests
# -----------------------------------------------------------
Write-Step 'STEP 9 — Auth tests'

Write-File 'tests\MojRok.Tests\Auth\PasswordHasherTests.cs' @'
using Xunit;
using MojRok.Infrastructure.Auth;

namespace MojRok.Tests.Auth;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ReturnsNonEmptyString()
    {
        var hash = _hasher.Hash("SecurePass123!");
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void Hash_ReturnsDifferentHashEachTime()
    {
        // BCrypt uses a random salt — same password, different hash every time.
        var hash1 = _hasher.Hash("SecurePass123!");
        var hash2 = _hasher.Hash("SecurePass123!");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_ReturnsTrueForCorrectPassword()
    {
        var hash = _hasher.Hash("SecurePass123!");
        Assert.True(_hasher.Verify("SecurePass123!", hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForWrongPassword()
    {
        var hash = _hasher.Hash("SecurePass123!");
        Assert.False(_hasher.Verify("WrongPassword!", hash));
    }
}
'@

Write-File 'tests\MojRok.Tests\Auth\JwtServiceTests.cs' @'
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using MojRok.Domain.Entities;
using MojRok.Domain.Enums;
using MojRok.Infrastructure.Auth;
using Xunit;

namespace MojRok.Tests.Auth;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]              = "test-secret-key-at-least-32-characters-long!",
                ["Jwt:Issuer"]           = "MojRok",
                ["Jwt:Audience"]         = "MojRok",
                ["Jwt:ExpiresInMinutes"] = "60"
            })
            .Build();

        _jwtService = new JwtService(config);
    }

    private static AppUser MakeUser() => new()
    {
        Id       = Guid.NewGuid(),
        Email    = "test@mojrok.mk",
        FullName = "Test User",
        Role     = UserRole.Citizen,
        IsActive = true
    };

    [Fact]
    public void GenerateToken_ReturnsNonEmptyToken()
    {
        var (token, _) = _jwtService.GenerateToken(MakeUser());
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_ExpiresAt_IsInFuture()
    {
        var (_, expiresAt) = _jwtService.GenerateToken(MakeUser());
        Assert.True(expiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_ContainsCorrectSubjectClaim()
    {
        var user = MakeUser();
        var (token, _) = _jwtService.GenerateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(user.Id.ToString(), jwt.Subject);
    }

    [Fact]
    public void GenerateToken_IssuerAndAudienceAreCorrect()
    {
        var (token, _) = _jwtService.GenerateToken(MakeUser());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("MojRok", jwt.Issuer);
        Assert.Contains("MojRok", jwt.Audiences);
    }
}
'@

Write-File 'tests\MojRok.Tests\Auth\AuthServiceTests.cs' @'
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MojRok.Application.Auth;
using MojRok.Application.Auth.DTOs;
using MojRok.Application.Exceptions;
using MojRok.Domain.Entities;
using MojRok.Domain.Enums;
using MojRok.Infrastructure.Auth;
using MojRok.Infrastructure.Persistence;
using Xunit;

namespace MojRok.Tests.Auth;

/// <summary>
/// Tests for AuthService using EF Core InMemory provider.
/// Each test gets a fresh database (Guid-named) to avoid state leakage.
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly AuthService  _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]              = "test-secret-key-at-least-32-characters-long!",
                ["Jwt:Issuer"]           = "MojRok",
                ["Jwt:Audience"]         = "MojRok",
                ["Jwt:ExpiresInMinutes"] = "60"
            })
            .Build();

        _authService = new AuthService(
            _db,
            new PasswordHasher(),
            new JwtService(config));
    }

    public void Dispose() => _db.Dispose();

    // ---- Register ----

    [Fact]
    public async Task Register_ReturnsAuthResponse_WithValidData()
    {
        var response = await _authService.RegisterAsync(new RegisterRequest
        {
            Email    = "darko@mojrok.mk",
            Password = "SecurePass123!",
            FullName = "Darko Test"
        });

        Assert.NotEmpty(response.Token);
        Assert.Equal("Darko Test", response.FullName);
        Assert.Equal("Citizen", response.Role);
        Assert.True(response.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_NormalizesEmail_ToLowercase()
    {
        await _authService.RegisterAsync(new RegisterRequest
        {
            Email    = "DARKO@MojRok.MK",
            Password = "SecurePass123!",
            FullName = "Darko Test"
        });

        var user = await _db.Users.FirstAsync();
        Assert.Equal("darko@mojrok.mk", user.Email);
    }

    [Fact]
    public async Task Register_Throws_WhenEmailAlreadyExists()
    {
        var request = new RegisterRequest
        {
            Email    = "darko@mojrok.mk",
            Password = "SecurePass123!",
            FullName = "Darko Test"
        };

        await _authService.RegisterAsync(request);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task Register_Throws_WhenDuplicateEmail_DifferentCase()
    {
        await _authService.RegisterAsync(new RegisterRequest
        {
            Email    = "darko@mojrok.mk",
            Password = "SecurePass123!",
            FullName = "Darko Test"
        });

        // Same email, different casing — should still be rejected
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync(new RegisterRequest
            {
                Email    = "DARKO@mojrok.mk",
                Password = "AnotherPass123!",
                FullName = "Darko Test 2"
            }));
    }

    // ---- Login ----

    [Fact]
    public async Task Login_ReturnsAuthResponse_WithValidCredentials()
    {
        await _authService.RegisterAsync(new RegisterRequest
        {
            Email    = "darko@mojrok.mk",
            Password = "SecurePass123!",
            FullName = "Darko Test"
        });

        var response = await _authService.LoginAsync(new LoginRequest
        {
            Email    = "darko@mojrok.mk",
            Password = "SecurePass123!"
        });

        Assert.NotEmpty(response.Token);
        Assert.Equal("Darko Test", response.FullName);
    }

    [Fact]
    public async Task Login_Throws_WhenPasswordIsWrong()
    {
        await _authService.RegisterAsync(new RegisterRequest
        {
            Email    = "darko@mojrok.mk",
            Password = "SecurePass123!",
            FullName = "Darko Test"
        });

        await Assert.ThrowsAsync<AuthException>(
            () => _authService.LoginAsync(new LoginRequest
            {
                Email    = "darko@mojrok.mk",
                Password = "WrongPassword!"
            }));
    }

    [Fact]
    public async Task Login_Throws_WhenEmailNotFound()
    {
        await Assert.ThrowsAsync<AuthException>(
            () => _authService.LoginAsync(new LoginRequest
            {
                Email    = "nobody@mojrok.mk",
                Password = "AnyPassword123!"
            }));
    }

    [Fact]
    public async Task Login_Throws_WhenUserIsInactive()
    {
        _db.Users.Add(new AppUser
        {
            Id           = Guid.NewGuid(),
            Email        = "inactive@mojrok.mk",
            PasswordHash = new PasswordHasher().Hash("SecurePass123!"),
            FullName     = "Inactive User",
            Role         = UserRole.Citizen,
            IsActive     = false,
            CreatedAt    = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<AuthException>(
            () => _authService.LoginAsync(new LoginRequest
            {
                Email    = "inactive@mojrok.mk",
                Password = "SecurePass123!"
            }));
    }
}
'@

# -----------------------------------------------------------
# STEP 10 — Configure user-secrets for JWT Key
# -----------------------------------------------------------
Write-Step 'STEP 10 — Initializing user-secrets'

dotnet user-secrets init --project src\MojRok.API 2>&1 | Out-Null

# Generate a cryptographically random 64-char key
Add-Type -AssemblyName System.Security
$bytes = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)

dotnet user-secrets set "Jwt:Key" $jwtKey --project src\MojRok.API
Write-Done "JWT Key saved to user-secrets (not shown)"
Write-Done "Key length: $($jwtKey.Length) characters"

# -----------------------------------------------------------
# STEP 11 — restore, build, test
# -----------------------------------------------------------
Write-Step 'STEP 11 — dotnet restore'
dotnet restore
Write-Done 'Restore complete'

Write-Step 'STEP 11 — dotnet build --configuration Release'
dotnet build --no-restore --configuration Release
Write-Done 'Build complete'

Write-Step 'STEP 11 — dotnet test --configuration Release'
dotnet test --no-build --configuration Release --verbosity normal
Write-Done 'Tests complete'

# -----------------------------------------------------------
Write-Host ''
Write-Host '=== Phase 2 Complete ===' -ForegroundColor Yellow
Write-Host ''
Write-Host 'Test via Swagger:' -ForegroundColor Cyan
Write-Host '  1. dotnet run --project src/MojRok.API'
Write-Host '  2. Open https://localhost:<port>/swagger'
Write-Host '  3. POST /api/auth/register  -> copy the token'
Write-Host '  4. Click Authorize -> paste: Bearer <token>'
Write-Host '  5. POST /api/auth/login  -> confirm same token flow'
Write-Host ''
Write-Host 'To view/change the JWT key:' -ForegroundColor Cyan
Write-Host '  dotnet user-secrets list --project src/MojRok.API'
Write-Host ''

