using System.ComponentModel.DataAnnotations;

namespace MojRok.Application.Users.DTOs;

/// <summary>
/// Accepted fields for PUT /api/users/me.
/// Email, PasswordHash, Role, IsActive, Id and CreatedAt are never accepted here.
/// </summary>
public class UpdateUserProfileRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    public int? MunicipalityId { get; set; }

    [Required]
    [RegularExpression("^(mk|en)$", ErrorMessage = "PreferredLanguage must be 'mk' or 'en'.")]
    public string PreferredLanguage { get; set; } = "mk";
}
