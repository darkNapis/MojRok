namespace MojRok.Application.Services.DTOs;

/// <summary>
/// Both mk and en fields are always returned.
/// Client picks the language based on the user's PreferredLanguage.
/// </summary>
public class CitizenServiceResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameMk { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DescriptionMk { get; set; }
    public int ServiceCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryNameMk { get; set; } = string.Empty;
    public int? MunicipalityId { get; set; }
    public string? MunicipalityName { get; set; }
    public string? MunicipalityNameMk { get; set; }
    public string WebsiteUrl { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
}