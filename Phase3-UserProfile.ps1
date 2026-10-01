# =============================================================
# MojRok — Phase 3: User Profile
# Run from: C:\Users\APIS\OneDrive\Desktop\MojRok
# Prerequisites: Phase 1 + Phase 2 completed and verified.
#
# Changes made by this script:
#   NEW  Application/Exceptions/NotFoundException.cs
#   NEW  Application/Users/DTOs/UserProfileResponse.cs
#   NEW  Application/Users/DTOs/UpdateUserProfileRequest.cs
#   NEW  Application/Users/UserService.cs
#   NEW  API/Controllers/UsersController.cs
#   NEW  tests/MojRok.Tests/Users/UserServiceTests.cs
#   PATCH API/Program.cs  (4 targeted additions — rest preserved)
#
# No new NuGet packages.
# No database migration (AppUser schema unchanged).
# No git commands.
# =============================================================

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step  { param($msg) Write-Host "`n>>> $msg" -ForegroundColor Cyan }
function Write-Done  { param($msg) Write-Host "    [OK] $msg" -ForegroundColor Green }
function Write-Info  { param($msg) Write-Host "    [--] $msg" -ForegroundColor Gray }
function Write-Fatal { param($msg) Write-Host "`n[FATAL] $msg" -ForegroundColor Red; exit 1 }

if (-not (Test-Path 'MojRok.sln')) {
    Write-Fatal 'MojRok.sln not found. Run from C:\Users\APIS\OneDrive\Desktop\MojRok'
}
if (-not (Test-Path 'src\MojRok.API\Program.cs')) {
    Write-Fatal 'src\MojRok.API\Program.cs not found. Complete Phase 2 first.'
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

Write-Host "`n=== MojRok Phase 3 — User Profile ===" -ForegroundColor Yellow

# -----------------------------------------------------------
# STEP 1 — Application: NotFoundException
# -----------------------------------------------------------
Write-Step 'STEP 1 — Application: NotFoundException'

Write-File 'src\MojRok.Application\Exceptions\NotFoundException.cs' @'
namespace MojRok.Application.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist.
/// Maps to HTTP 404 in the API layer.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
'@

# -----------------------------------------------------------
# STEP 2 — Application: User DTOs
# -----------------------------------------------------------
Write-Step 'STEP 2 — Application: User DTOs'

Write-File 'src\MojRok.Application\Users\DTOs\UserProfileResponse.cs' @'
namespace MojRok.Application.Users.DTOs;

/// <summary>
/// Returned by GET /api/users/me and PUT /api/users/me.
/// PasswordHash is intentionally excluded.
/// </summary>
public class UserProfileResponse
{
    public Guid    Id                { get; set; }
    public string  Email             { get; set; } = string.Empty;
    public string  FullName          { get; set; } = string.Empty;
    public string? PhoneNumber       { get; set; }
    public int?    MunicipalityId    { get; set; }
    public string  PreferredLanguage { get; set; } = string.Empty;
    public string  Role              { get; set; } = string.Empty;
}
'@

Write-File 'src\MojRok.Application\Users\DTOs\UpdateUserProfileRequest.cs' @'
using System.ComponentModel.DataAnnotations;

namespace MojRok.Application.Users.DTOs;

/// <summary>
/// Accepted fields for PUT /api/users/me.
/// Email, PasswordHash, Role, IsActive, Id and CreatedAt are never accepted here.
/// </summary>
public class UpdateUserProfileRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    public int? MunicipalityId { get; set; }

    [Required]
    [RegularExpression("^(mk|en)$", ErrorMessage = "PreferredLanguage must be 'mk' or 'en'.")]
    public string PreferredLanguage { get; set; } = "mk";
}
'@

# -----------------------------------------------------------
# STEP 3 — Application: UserService
# -----------------------------------------------------------
Write-Step 'STEP 3 — Application: UserService'

Write-File 'src\MojRok.Application\Users\UserService.cs' @'
using Microsoft.EntityFrameworkCore;
using MojRok.Application.Abstractions;
using MojRok.Application.Exceptions;
using MojRok.Application.Users.DTOs;
using MojRok.Domain.Entities;

namespace MojRok.Application.Users;

public class UserService
{
    private readonly IAppDbContext _db;

    public UserService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfileResponse> GetMyProfileAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            throw new NotFoundException($"User {userId} was not found.");

        return ToResponse(user);
    }

    public async Task<UserProfileResponse> UpdateMyProfileAsync(
        Guid userId,
        UpdateUserProfileRequest request,
        CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            throw new NotFoundException($"User {userId} was not found.");

        // Only update the fields the user is permitted to change.
        // Email, PasswordHash, Role, IsActive, CreatedAt and Id are never modified here.
        user.FullName          = request.FullName.Trim();
        user.PhoneNumber       = request.PhoneNumber?.Trim();
        user.MunicipalityId    = request.MunicipalityId;
        user.PreferredLanguage = request.PreferredLanguage.ToLowerInvariant();

        await _db.SaveChangesAsync(ct);

        return ToResponse(user);
    }

    private static UserProfileResponse ToResponse(AppUser user) => new()
    {
        Id                = user.Id,
        Email             = user.Email,
        FullName          = user.FullName,
        PhoneNumber       = user.PhoneNumber,
        MunicipalityId    = user.MunicipalityId,
        PreferredLanguage = user.PreferredLanguage,
        Role              = user.Role.ToString()
    };
}
'@

# -----------------------------------------------------------
# STEP 4 — Patch Program.cs (targeted, preserves Phase 2 config)
#
# Four additions:
#   1. using MojRok.Application.Users;
#   2. builder.Services.AddScoped<UserService>();
#   3. options.MapInboundClaims = false;
#      Makes JwtBearer preserve claim names as-is so
#      User.FindFirst("sub") works without depending on
#      the default ClaimTypes.NameIdentifier remapping.
#   4. RoleClaimType = "role"
#      Required so that [Authorize(Roles = "Admin")] works
#      correctly after MapInboundClaims is disabled.
#
# All existing Phase 2 content is read first and preserved.
# -----------------------------------------------------------
Write-Step 'STEP 4 — Patching Program.cs'

$programPath = 'src\MojRok.API\Program.cs'
$prog = Get-Content $programPath -Raw

Write-Info "Read $programPath ($($prog.Length) chars)"

# 1 — using MojRok.Application.Users
if ($prog -notmatch 'using MojRok\.Application\.Users;') {
    $prog = $prog -replace 'using MojRok\.Application\.Auth;',
        "using MojRok.Application.Auth;`r`nusing MojRok.Application.Users;"
    if ($prog -notmatch 'using MojRok\.Application\.Users;') {
        Write-Fatal "Could not insert 'using MojRok.Application.Users' into Program.cs. Check Phase 2 content."
    }
    Write-Done 'Added: using MojRok.Application.Users'
} else {
    Write-Info 'Already present: using MojRok.Application.Users'
}

# 2 — AddScoped<UserService>
if ($prog -notmatch 'AddScoped<UserService>') {
    $prog = $prog -replace 'AddScoped<AuthService>\(\);',
        "AddScoped<AuthService>();`r`nbuilder.Services.AddScoped<UserService>();"
    if ($prog -notmatch 'AddScoped<UserService>') {
        Write-Fatal "Could not insert 'AddScoped<UserService>' into Program.cs. Check Phase 2 content."
    }
    Write-Done 'Added: builder.Services.AddScoped<UserService>()'
} else {
    Write-Info 'Already present: AddScoped<UserService>'
}

# 3 — MapInboundClaims
if ($prog -notmatch 'options\.MapInboundClaims\s*=\s*false') {
    $newProg = $prog -replace '([ \t]+options\.TokenValidationParameters\s*=)', {
        "        options.MapInboundClaims = false;`r`n`r`n$($args[0].Value)"
    }

    if ($newProg -eq $prog) {
        throw "FAILED: Could not add options.MapInboundClaims = false"
    }

    $prog = $newProg
    Write-Host "    [OK] Added: options.MapInboundClaims = false" -ForegroundColor Green
}
else {
    Write-Host "    [--] options.MapInboundClaims already exists" -ForegroundColor DarkGray
}

# 4 — RoleClaimType
if ($prog -notmatch 'RoleClaimType\s*=\s*"role"') {
    $newProg = $prog -replace '([ \t]+ClockSkew\s*=\s*TimeSpan\.Zero)', {
        "            RoleClaimType            = `"role`",`r`n$($args[0].Value)"
    }

    if ($newProg -eq $prog) {
        throw "FAILED: Could not add RoleClaimType = `"role`""
    }

    $prog = $newProg
    Write-Host '    [OK] Added: RoleClaimType = "role"' -ForegroundColor Green
}
else {
    Write-Host "    [--] RoleClaimType already exists" -ForegroundColor DarkGray
}

Set-Content -Path $programPath -Value $prog -Encoding UTF8
Write-Done "Program.cs saved"

# -----------------------------------------------------------
# STEP 5 — API: UsersController
#
# Claim extraction:
#   With MapInboundClaims = false, the "sub" claim in the JWT
#   is NOT remapped — it remains "sub" in the principal.
#   User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value is
#   therefore deterministic and does not rely on default mapping.
# -----------------------------------------------------------
Write-Step 'STEP 5 — API: UsersController'

Write-File 'src\MojRok.API\Controllers\UsersController.cs' @'
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MojRok.Application.Exceptions;
using MojRok.Application.Users;
using MojRok.Application.Users.DTOs;

namespace MojRok.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Returns the profile of the currently authenticated user.
    /// User ID is extracted from the JWT "sub" claim — never from the URL.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var profile = await _userService.GetMyProfileAsync(userId.Value, ct);
            return Ok(profile);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Updates the editable fields of the currently authenticated user's profile.
    /// Protected fields (Email, Role, PasswordHash, IsActive, CreatedAt, Id) are ignored.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var profile = await _userService.UpdateMyProfileAsync(userId.Value, request, ct);
            return Ok(profile);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Extracts the authenticated user's Guid from the JWT "sub" claim.
    ///
    /// Relies on options.MapInboundClaims = false (configured in Program.cs),
    /// which keeps "sub" as its original JWT name and makes this deterministic.
    /// Without that setting, JwtBearer would remap "sub" to
    /// ClaimTypes.NameIdentifier and this lookup would return null.
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
'@

# -----------------------------------------------------------
# STEP 6 — Tests: UserServiceTests (12 tests)
# -----------------------------------------------------------
Write-Step 'STEP 6 — Tests: UserServiceTests'

Write-File 'tests\MojRok.Tests\Users\UserServiceTests.cs' @'
using Microsoft.EntityFrameworkCore;
using MojRok.Application.Exceptions;
using MojRok.Application.Users;
using MojRok.Application.Users.DTOs;
using MojRok.Domain.Entities;
using MojRok.Domain.Enums;
using MojRok.Infrastructure.Persistence;
using Xunit;

namespace MojRok.Tests.Users;

/// <summary>
/// Unit tests for UserService using the EF Core InMemory provider.
/// Each test gets a fresh isolated database (Guid-named) to prevent state leakage.
/// </summary>
public class UserServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly UserService  _userService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db          = new AppDbContext(options);
        _userService = new UserService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ---- Seed helper -----------------------------------------------

    private async Task<AppUser> SeedUserAsync(
        string   email             = "darko@mojrok.mk",
        string   fullName          = "Darko Nikolic",
        string?  phoneNumber       = "+38970123456",
        int?     municipalityId    = null,
        string   preferredLanguage = "mk",
        UserRole role              = UserRole.Citizen,
        bool     isActive          = true)
    {
        var user = new AppUser
        {
            Id                = Guid.NewGuid(),
            Email             = email,
            PasswordHash      = "bcrypt_hashed_password_not_plaintext",
            FullName          = fullName,
            PhoneNumber       = phoneNumber,
            MunicipalityId    = municipalityId,
            PreferredLanguage = preferredLanguage,
            Role              = role,
            IsActive          = isActive,
            CreatedAt         = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    // ================================================================
    // GET profile
    // ================================================================

    [Fact]
    public async Task GetProfile_ReturnsCurrentUser()
    {
        var user   = await SeedUserAsync();
        var result = await _userService.GetMyProfileAsync(user.Id);

        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetProfile_ReturnsCorrectData()
    {
        var user   = await SeedUserAsync(phoneNumber: "+38971555444", municipalityId: 5);
        var result = await _userService.GetMyProfileAsync(user.Id);

        Assert.Equal(user.Email,             result.Email);
        Assert.Equal(user.FullName,          result.FullName);
        Assert.Equal(user.PhoneNumber,       result.PhoneNumber);
        Assert.Equal(user.MunicipalityId,    result.MunicipalityId);
        Assert.Equal(user.PreferredLanguage, result.PreferredLanguage);
        Assert.Equal(user.Role.ToString(),   result.Role);
    }

    [Fact]
    public async Task GetProfile_DoesNotExposePasswordHash()
    {
        var user   = await SeedUserAsync();
        var result = await _userService.GetMyProfileAsync(user.Id);

        // UserProfileResponse must not have any property related to the password.
        var properties = result.GetType().GetProperties();
        Assert.DoesNotContain(properties,
            p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetProfile_ThrowsWhenUserDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _userService.GetMyProfileAsync(Guid.NewGuid()));
    }

    // ================================================================
    // UPDATE profile
    // ================================================================

    [Fact]
    public async Task UpdateProfile_UpdatesAllowedFields()
    {
        var user    = await SeedUserAsync();
        var request = new UpdateUserProfileRequest
        {
            FullName          = "Ana Petrovic",
            PhoneNumber       = "+38971888777",
            MunicipalityId    = 3,
            PreferredLanguage = "en"
        };

        var result = await _userService.UpdateMyProfileAsync(user.Id, request);

        Assert.Equal("Ana Petrovic",  result.FullName);
        Assert.Equal("+38971888777",  result.PhoneNumber);
        Assert.Equal(3,               result.MunicipalityId);
        Assert.Equal("en",            result.PreferredLanguage);
    }

    [Fact]
    public async Task UpdateProfile_DoesNotChangeEmail()
    {
        var user = await SeedUserAsync(email: "original@mojrok.mk");

        var result = await _userService.UpdateMyProfileAsync(user.Id,
            new UpdateUserProfileRequest { FullName = "New Name", PreferredLanguage = "mk" });

        Assert.Equal("original@mojrok.mk", result.Email);
    }

    [Fact]
    public async Task UpdateProfile_DoesNotChangeRole()
    {
        var user = await SeedUserAsync(role: UserRole.Citizen);

        var result = await _userService.UpdateMyProfileAsync(user.Id,
            new UpdateUserProfileRequest { FullName = "New Name", PreferredLanguage = "mk" });

        Assert.Equal("Citizen", result.Role);
    }

    [Fact]
    public async Task UpdateProfile_DoesNotChangeIsActive()
    {
        var user = await SeedUserAsync(isActive: true);

        await _userService.UpdateMyProfileAsync(user.Id,
            new UpdateUserProfileRequest { FullName = "New Name", PreferredLanguage = "mk" });

        var stored = await _db.Users.FirstAsync(u => u.Id == user.Id);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task UpdateProfile_DoesNotChangePasswordHash()
    {
        var user         = await SeedUserAsync();
        var originalHash = user.PasswordHash;

        await _userService.UpdateMyProfileAsync(user.Id,
            new UpdateUserProfileRequest { FullName = "New Name", PreferredLanguage = "mk" });

        var stored = await _db.Users.FirstAsync(u => u.Id == user.Id);
        Assert.Equal(originalHash, stored.PasswordHash);
    }

    [Fact]
    public async Task UpdateProfile_DoesNotChangeCreatedAt()
    {
        var user            = await SeedUserAsync();
        var originalCreated = user.CreatedAt;

        await _userService.UpdateMyProfileAsync(user.Id,
            new UpdateUserProfileRequest { FullName = "New Name", PreferredLanguage = "mk" });

        var stored = await _db.Users.FirstAsync(u => u.Id == user.Id);
        Assert.Equal(originalCreated, stored.CreatedAt);
    }

    [Fact]
    public async Task UpdateProfile_NormalizesPreferredLanguage()
    {
        var user = await SeedUserAsync();

        var result = await _userService.UpdateMyProfileAsync(user.Id,
            new UpdateUserProfileRequest { FullName = "New Name", PreferredLanguage = "EN" });

        Assert.Equal("en", result.PreferredLanguage);
    }

    [Fact]
    public async Task UpdateProfile_TrimsFullName()
    {
        var user = await SeedUserAsync();

        var result = await _userService.UpdateMyProfileAsync(user.Id,
            new UpdateUserProfileRequest
            {
                FullName          = "  Darko Nikolic  ",
                PreferredLanguage = "mk"
            });

        Assert.Equal("Darko Nikolic", result.FullName);
    }
}
'@

# -----------------------------------------------------------
# STEP 7 — restore, build, test
# -----------------------------------------------------------
Write-Step 'STEP 7 — dotnet restore'
dotnet restore
if ($LASTEXITCODE -ne 0) { Write-Fatal 'dotnet restore failed.' }
Write-Done 'Restore succeeded'

Write-Step 'STEP 7 — dotnet build --configuration Release'
dotnet build --no-restore --configuration Release
if ($LASTEXITCODE -ne 0) { Write-Fatal 'dotnet build failed.' }
Write-Done 'Build succeeded'

Write-Step 'STEP 7 — dotnet test --configuration Release'
dotnet test --no-build --configuration Release --verbosity normal
if ($LASTEXITCODE -ne 0) { Write-Fatal 'dotnet test failed.' }
Write-Done 'Tests succeeded'

# -----------------------------------------------------------
Write-Host ''
Write-Host '=== Phase 3 Complete ===' -ForegroundColor Yellow
Write-Host ''
Write-Host 'Swagger manual verification:' -ForegroundColor Cyan
Write-Host '  dotnet run --project src/MojRok.API'
Write-Host ''
Write-Host '  1. GET  /api/Users/me (no token)         -> 401'
Write-Host '  2. POST /api/Auth/login                  -> copy token'
Write-Host '  3. Authorize: Bearer <token>'
Write-Host '  4. GET  /api/Users/me                    -> 200, no passwordHash'
Write-Host '  5. PUT  /api/Users/me (fullName etc.)    -> 200, email/role unchanged'
Write-Host '  6. PUT  /api/Users/me preferredLanguage="de" -> 400'
Write-Host ''
