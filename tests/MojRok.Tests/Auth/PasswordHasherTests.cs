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
        // BCrypt uses a random salt â€” same password, different hash every time.
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
