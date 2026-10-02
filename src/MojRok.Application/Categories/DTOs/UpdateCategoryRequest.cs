using System.ComponentModel.DataAnnotations;

namespace MojRok.Application.Categories.DTOs;

public class UpdateCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(7)]
    public string Color { get; set; } = "#6366F1";

    [MaxLength(50)]
    public string? IconSlug { get; set; }
}
