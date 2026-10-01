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
