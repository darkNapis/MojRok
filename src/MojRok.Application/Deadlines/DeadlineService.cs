using Microsoft.EntityFrameworkCore;
using MojRok.Application.Abstractions;
using MojRok.Application.Deadlines.DTOs;
using MojRok.Application.Exceptions;
using MojRok.Domain.Entities;

namespace MojRok.Application.Deadlines;

public class DeadlineService
{
    private readonly IAppDbContext _db;

    public DeadlineService(IAppDbContext db)
    {
        _db = db;
    }

    // ----------------------------------------------------------------
    // GET /api/deadlines
    // ----------------------------------------------------------------
    /// <summary>
    /// Returns all deadlines belonging to the current user, newest first.
    /// Uses a single JOIN query - no N+1 queries.
    /// </summary>
    public async Task<List<DeadlineResponse>> GetAllAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var deadlines = await _db.Deadlines
            .Include(d => d.Category)
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        return deadlines.Select(ToResponse).ToList();
    }

    // ----------------------------------------------------------------
    // GET /api/deadlines/{id}
    // ----------------------------------------------------------------
    /// <summary>
    /// Returns a single deadline. Throws NotFoundException if the deadline
    /// does not exist OR belongs to a different user (prevents data leakage).
    /// </summary>
    public async Task<DeadlineResponse> GetByIdAsync(
        Guid userId,
        Guid deadlineId,
        CancellationToken ct = default)
    {
        var deadline = await _db.Deadlines
            .Include(d => d.Category)
            .FirstOrDefaultAsync(d => d.Id == deadlineId && d.UserId == userId, ct);

        if (deadline is null)
            throw new NotFoundException($"Deadline {deadlineId} was not found.");

        return ToResponse(deadline);
    }

    // ----------------------------------------------------------------
    // POST /api/deadlines
    // ----------------------------------------------------------------
    /// <summary>
    /// Creates a new deadline for the current user.
    /// UserId is always set from the service parameter - never from the request.
    /// IsCompleted and CompletedAt are always initialised to false/null by the server.
    /// </summary>
    public async Task<DeadlineResponse> CreateAsync(
        Guid userId,
        CreateDeadlineRequest request,
        CancellationToken ct = default)
    {
        ValidateRecurrence(request.IsRecurring, request.RecurrenceIntervalDays);

        // Validates existence and user access, returns the Category entity
        var category = await GetAccessibleCategoryAsync(userId, request.CategoryId, ct);

        var now = DateTime.UtcNow;
        var deadline = new Deadline
        {
            Id                     = Guid.NewGuid(),
            Title                  = request.Title.Trim(),
            Description            = request.Description?.Trim(),
            DueDate                = request.DueDate,
            UserId                 = userId,
            CategoryId             = request.CategoryId,
            IsRecurring            = request.IsRecurring,
            RecurrenceIntervalDays = request.IsRecurring ? request.RecurrenceIntervalDays : null,
            IsCompleted            = false,
            CompletedAt            = null,
            CreatedAt              = now,
            UpdatedAt              = now
        };

        _db.Deadlines.Add(deadline);
        await _db.SaveChangesAsync(ct);

        // Set navigation for response without a second DB round-trip
        deadline.Category = category;

        return ToResponse(deadline);
    }

    // ----------------------------------------------------------------
    // PUT /api/deadlines/{id}
    // ----------------------------------------------------------------
    /// <summary>
    /// Updates editable fields of the current user's deadline.
    /// Id, UserId, CreatedAt, IsCompleted and CompletedAt are never changed here.
    /// </summary>
    public async Task<DeadlineResponse> UpdateAsync(
        Guid userId,
        Guid deadlineId,
        UpdateDeadlineRequest request,
        CancellationToken ct = default)
    {
        ValidateRecurrence(request.IsRecurring, request.RecurrenceIntervalDays);

        var deadline = await _db.Deadlines
            .FirstOrDefaultAsync(d => d.Id == deadlineId && d.UserId == userId, ct);

        if (deadline is null)
            throw new NotFoundException($"Deadline {deadlineId} was not found.");

        var category = await GetAccessibleCategoryAsync(userId, request.CategoryId, ct);

        deadline.Title                  = request.Title.Trim();
        deadline.Description            = request.Description?.Trim();
        deadline.DueDate                = request.DueDate;
        deadline.CategoryId             = request.CategoryId;
        deadline.IsRecurring            = request.IsRecurring;
        deadline.RecurrenceIntervalDays = request.IsRecurring ? request.RecurrenceIntervalDays : null;
        deadline.UpdatedAt              = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // Set navigation for response without reloading
        deadline.Category = category;

        return ToResponse(deadline);
    }

    // ----------------------------------------------------------------
    // DELETE /api/deadlines/{id}
    // ----------------------------------------------------------------
    /// <summary>
    /// Deletes the current user's deadline.
    /// Returns NotFoundException for both missing and other-user cases
    /// to prevent data leakage.
    /// </summary>
    public async Task DeleteAsync(
        Guid userId,
        Guid deadlineId,
        CancellationToken ct = default)
    {
        var deadline = await _db.Deadlines
            .FirstOrDefaultAsync(d => d.Id == deadlineId && d.UserId == userId, ct);

        if (deadline is null)
            throw new NotFoundException($"Deadline {deadlineId} was not found.");

        _db.Deadlines.Remove(deadline);
        await _db.SaveChangesAsync(ct);
    }

    // ----------------------------------------------------------------
    // POST /api/deadlines/{id}/complete
    // ----------------------------------------------------------------
    /// <summary>
    /// Marks the current user's deadline as completed.
    /// Idempotent: calling this on an already-completed deadline is safe
    /// and returns the current state without error.
    /// </summary>
    public async Task<DeadlineResponse> CompleteAsync(
        Guid userId,
        Guid deadlineId,
        CancellationToken ct = default)
    {
        var deadline = await _db.Deadlines
            .Include(d => d.Category)
            .FirstOrDefaultAsync(d => d.Id == deadlineId && d.UserId == userId, ct);

        if (deadline is null)
            throw new NotFoundException($"Deadline {deadlineId} was not found.");

        if (!deadline.IsCompleted)
        {
            var now = DateTime.UtcNow;
            deadline.IsCompleted = true;
            deadline.CompletedAt = now;
            deadline.UpdatedAt   = now;
            await _db.SaveChangesAsync(ct);
        }

        return ToResponse(deadline);
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    /// <summary>
    /// Verifies that a category is accessible to the current user:
    /// either a system default (UserId == null) or owned by this user.
    /// Throws NotFoundException in both "not found" and "belongs to another user"
    /// cases to prevent data leakage.
    /// </summary>
    private async Task<Category> GetAccessibleCategoryAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken ct)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

        if (category is null)
            throw new NotFoundException("Category not found.");

        // UserId == null -> system default, accessible to everyone
        // UserId == userId -> user's own category, accessible
        // UserId != null and != userId -> another user's category, not accessible
        if (category.UserId != null && category.UserId != userId)
            throw new NotFoundException("Category not found.");

        return category;
    }

    /// <summary>
    /// Service-level guard for recurrence rules.
    /// Controller ModelState validation catches this first for HTTP requests,
    /// but the service enforces the rule independently for testability.
    /// </summary>
    private static void ValidateRecurrence(bool isRecurring, int? intervalDays)
    {
        if (isRecurring && (intervalDays is null or < 1))
            throw new ArgumentException(
                "RecurrenceIntervalDays must be a positive integer when IsRecurring is true.",
                nameof(intervalDays));
    }

    private static DeadlineResponse ToResponse(Deadline d) => new()
    {
        Id                     = d.Id,
        Title                  = d.Title,
        Description            = d.Description,
        DueDate                = d.DueDate,
        Status                 = d.Status.ToString(),
        CategoryId             = d.CategoryId,
        CategoryName           = d.Category?.Name ?? string.Empty,
        IsRecurring            = d.IsRecurring,
        RecurrenceIntervalDays = d.RecurrenceIntervalDays,
        IsCompleted            = d.IsCompleted,
        CompletedAt            = d.CompletedAt,
        CreatedAt              = d.CreatedAt,
        UpdatedAt              = d.UpdatedAt
    };
}
