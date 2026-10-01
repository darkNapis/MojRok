namespace MojRok.Application.Users.DTOs;

/// <summary>
/// Returned by GET /api/users/me and PUT /api/users/me.
/// PasswordHash is intentionally excluded.
/// </summary>
public class UserProfileResponse
{
    public Guid    Id                { get; set; }
    public string  Email             { get; set; } = string.Empty;
    public string  FullName          { get; set; } = string.Empty;
    public string? PhoneNumber       { get; set; }
    public int?    MunicipalityId    { get; set; }
    public string  PreferredLanguage { get; set; } = string.Empty;
    public string  Role              { get; set; } = string.Empty;
}
