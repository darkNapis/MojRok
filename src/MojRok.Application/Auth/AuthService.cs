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
