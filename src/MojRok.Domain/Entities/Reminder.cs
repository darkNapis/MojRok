using MojRok.Domain.Enums;

namespace MojRok.Domain.Entities;

public class Reminder
{
    public Guid Id { get; set; }
    public Guid DeadlineId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ReminderDate { get; set; }
    public ReminderChannel Channel { get; set; } = ReminderChannel.InApp;
    public bool IsSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Deadline Deadline { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
