using MojRok.Domain.Enums;

namespace MojRok.Application.Reminders.DTOs;

public class ReminderResponse
{
    public Guid Id { get; set; }
    public Guid DeadlineId { get; set; }
    public string DeadlineTitle { get; set; } = string.Empty;
    public DateTime ReminderDate { get; set; }
    public ReminderChannel Channel { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
}