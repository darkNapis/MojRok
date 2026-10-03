using System.ComponentModel.DataAnnotations;

namespace MojRok.Application.Services.DTOs;

public class UpdateCitizenServiceRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string NameMk { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? DescriptionMk { get; set; }

    [Required]
    public int ServiceCategoryId { get; set; }

    public int? MunicipalityId { get; set; }

    [Required, MaxLength(500)]
    public string WebsiteUrl { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;
}