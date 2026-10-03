using System.ComponentModel.DataAnnotations;

namespace MojRok.Application.Deadlines.DTOs;

/// <summary>
/// Accepted fields for PUT /api/deadlines/{id}.
/// Id, UserId, CreatedAt, IsCompleted, CompletedAt and Status
/// cannot be changed through this request.
/// IsCompleted is handled exclusively by POST /api/deadlines/{id}/complete.
/// </summary>
public class UpdateDeadlineRequest : IValidatableObject
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public bool IsRecurring { get; set; } = false;

    [Range(1, int.MaxValue, ErrorMessage = "RecurrenceIntervalDays must be at least 1.")]
    public int? RecurrenceIntervalDays { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (IsRecurring && (!RecurrenceIntervalDays.HasValue || RecurrenceIntervalDays.Value < 1))
        {
            yield return new ValidationResult(
                "RecurrenceIntervalDays must be a positive integer when IsRecurring is true.",
                new[] { nameof(RecurrenceIntervalDays) });
        }
    }
}
