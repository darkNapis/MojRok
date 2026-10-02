namespace MojRok.Application.Categories.DTOs;

public class CategoryResponse
{
    public Guid    Id        { get; set; }
    public string  Name      { get; set; } = string.Empty;
    public string  Color     { get; set; } = string.Empty;
    public string? IconSlug  { get; set; }
    public bool    IsDefault { get; set; }
    /// <summary>
    /// Null for system/default categories.
    /// Matches the authenticated user's Id for personal categories.
    /// </summary>
    public Guid?   UserId    { get; set; }
}
