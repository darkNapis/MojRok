using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MojRok.Application.Abstractions;
using MojRok.Domain.Entities;

namespace MojRok.Infrastructure.Auth;

public class JwtService : IJwtService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiresInMinutes;

    public JwtService(IConfiguration configuration)
    {
        _key             = configuration["Jwt:Key"]
                           ?? throw new InvalidOperationException(
                               "Jwt:Key is not configured. Use dotnet user-secrets.");
        _issuer          = configuration["Jwt:Issuer"]          ?? "MojRok";
        _audience        = configuration["Jwt:Audience"]        ?? "MojRok";
        _expiresInMinutes = int.TryParse(configuration["Jwt:ExpiresInMinutes"], out var m) ? m : 60;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(AppUser user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_expiresInMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,      user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email,    user.Email),
            new Claim(JwtRegisteredClaimNames.Jti,      Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role,                  user.Role.ToString()),
            new Claim("fullName",                       user.FullName)
        };

        var signingKey  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
