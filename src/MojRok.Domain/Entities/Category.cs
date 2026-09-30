namespace MojRok.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366F1";
    public string? IconSlug { get; set; }

    // Nullable: null = system/default category not owned by any specific user.
    // User-created categories have a non-null UserId.
    public Guid? UserId { get; set; }
    public bool IsDefault { get; set; } = false;

    // Navigation
    public AppUser? User { get; set; }
    public ICollection<Deadline> Deadlines { get; set; } = [];
}
