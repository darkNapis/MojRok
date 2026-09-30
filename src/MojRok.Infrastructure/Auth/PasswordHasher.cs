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
