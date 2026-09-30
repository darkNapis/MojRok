using MojRok.Domain.Enums;

namespace MojRok.Domain.Entities;

public class Deadline
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsRecurring { get; set; } = false;
    public int? RecurrenceIntervalDays { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public AppUser User { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<Reminder> Reminders { get; set; } = [];

    /// <summary>
    /// Computed from DueDate at runtime. Never stored in the database.
    /// EF Core configuration calls builder.Ignore(d => d.Status).
    /// Priority: Completed > Expired > Urgent > Upcoming > Active.
    /// </summary>
    public DeadlineStatus Status
    {
        get
        {
            if (IsCompleted) return DeadlineStatus.Completed;

            var today = DateTime.UtcNow.Date;
            var due   = DueDate.Date;

            if (due < today)              return DeadlineStatus.Expired;
            if (due <= today.AddDays(3))  return DeadlineStatus.Urgent;
            if (due <= today.AddDays(14)) return DeadlineStatus.Upcoming;

            return DeadlineStatus.Active;
        }
    }
}
