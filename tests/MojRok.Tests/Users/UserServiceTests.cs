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
