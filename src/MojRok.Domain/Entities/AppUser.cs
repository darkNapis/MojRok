using MojRok.Domain.Enums;

namespace MojRok.Domain.Entities;

public class AppUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int? MunicipalityId { get; set; }
    public string PreferredLanguage { get; set; } = "mk";
    public UserRole Role { get; set; } = UserRole.Citizen;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public Municipality? Municipality { get; set; }
    public ICollection<Deadline> Deadlines { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Reminder> Reminders { get; set; } = [];
}
