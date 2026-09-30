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
