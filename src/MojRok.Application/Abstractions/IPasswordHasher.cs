namespace MojRok.Application.Abstractions;

/// <summary>
/// Hashes and verifies passwords. Implemented in Infrastructure using BCrypt.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
