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
