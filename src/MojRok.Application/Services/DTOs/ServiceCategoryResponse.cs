namespace MojRok.Application.Services.DTOs;

public class ServiceCategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameMk { get; set; } = string.Empty;
    public string? IconSlug { get; set; }
    public int SortOrder { get; set; }
}