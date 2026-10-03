namespace MojRok.Application.Deadlines.DTOs;

/// <summary>
/// Returned by all Deadline endpoints.
/// UserId is intentionally excluded - clients only ever see their own deadlines.
/// Status is computed from DueDate at runtime and serialised as a string.
/// CategoryName is included to avoid a second round-trip from the client.
/// </summary>
public class DeadlineResponse
{
    public Guid     Id                    { get; set; }
    public string   Title                 { get; set; } = string.Empty;
    public string?  Description           { get; set; }
    public DateTime DueDate               { get; set; }
    public string   Status                { get; set; } = string.Empty;
    public Guid     CategoryId            { get; set; }
    public string   CategoryName          { get; set; } = string.Empty;
    public bool     IsRecurring           { get; set; }
    public int?     RecurrenceIntervalDays { get; set; }
    public bool     IsCompleted           { get; set; }
    public DateTime? CompletedAt          { get; set; }
    public DateTime CreatedAt             { get; set; }
    public DateTime UpdatedAt             { get; set; }
}
