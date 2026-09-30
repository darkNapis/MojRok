namespace MojRok.Domain.Entities;

public class CitizenService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameMk { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DescriptionMk { get; set; }
    public int ServiceCategoryId { get; set; }
    public int? MunicipalityId { get; set; }
    public string WebsiteUrl { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ServiceCategory ServiceCategory { get; set; } = null!;
    public Municipality? Municipality { get; set; }
}
