namespace MojRok.Domain.Entities;

public class Municipality
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameMk { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;

    // Navigation
    public ICollection<AppUser> Users { get; set; } = [];
    public ICollection<CitizenService> CitizenServices { get; set; } = [];
}
