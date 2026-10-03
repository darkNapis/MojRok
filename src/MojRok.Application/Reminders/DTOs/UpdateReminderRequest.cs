using System.ComponentModel.DataAnnotations;
using MojRok.Domain.Enums;

namespace MojRok.Application.Reminders.DTOs;

public class UpdateReminderRequest
{
    [Required]
    public DateTime ReminderDate { get; set; }

    public ReminderChannel Channel { get; set; } = ReminderChannel.InApp;
}