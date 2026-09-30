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

        // Same email, different casing â€” should still be rejected
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
