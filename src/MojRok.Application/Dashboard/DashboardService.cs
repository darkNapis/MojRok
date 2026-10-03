using Microsoft.EntityFrameworkCore;
using MojRok.Application.Abstractions;
using MojRok.Application.Dashboard.DTOs;
using MojRok.Application.Deadlines.DTOs;
using MojRok.Domain.Entities;

namespace MojRok.Application.Dashboard;

public class DashboardService
{
    private readonly IAppDbContext _db;

    public DashboardService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardResponse> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var in7Days = today.AddDays(7);
        var sevenDaysAgo = today.AddDays(-7);

        var deadlines = await _db.Deadlines
            .AsNoTracking()
            .Include(d => d.Category)
            .Where(d => d.UserId == userId)
            .ToListAsync(ct);

        var response = new DashboardResponse();

        // Total by status (uses the same computed Status as the rest of the app)
        response.TotalByStatus = deadlines
            .GroupBy(d => d.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Ensure all known statuses appear even if count is 0
        foreach (var status in new[] { "Expired", "Urgent", "Upcoming", "Active", "Completed" })
        {
            if (!response.TotalByStatus.ContainsKey(status))
                response.TotalByStatus[status] = 0;
        }

        // Upcoming in next 7 days (not completed, DueDate between today and today+7)
        response.UpcomingNext7Days = deadlines.Count(d =>
            !d.IsCompleted &&
            d.DueDate.Date >= today &&
            d.DueDate.Date <= in7Days);

        // Overdue = Expired (not completed and past due)
        response.OverdueCount = deadlines.Count(d =>
            !d.IsCompleted && d.DueDate.Date < today);

        // Recently completed = completed in the last 7 days
        response.RecentlyCompleted = deadlines.Count(d =>
            d.IsCompleted &&
            d.CompletedAt.HasValue &&
            d.CompletedAt.Value.Date >= sevenDaysAgo);

        // Next 3 urgent deadlines (status Urgent, ordered by DueDate ascending)
        response.Next3Urgent = deadlines
            .Where(d => !d.IsCompleted && d.Status.ToString() == "Urgent")
            .OrderBy(d => d.DueDate)
            .Take(3)
            .Select(ToDeadlineResponse)
            .ToList();

        return response;
    }

    private static DeadlineResponse ToDeadlineResponse(Deadline d) => new()
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