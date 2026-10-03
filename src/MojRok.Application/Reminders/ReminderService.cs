using Microsoft.EntityFrameworkCore;
using MojRok.Application.Abstractions;
using MojRok.Application.Exceptions;
using MojRok.Application.Reminders.DTOs;
using MojRok.Domain.Entities;
using MojRok.Domain.Enums;

namespace MojRok.Application.Reminders;

public class ReminderService
{
    private readonly IAppDbContext _db;

    public ReminderService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ReminderResponse>> GetAllAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var reminders = await _db.Reminders
            .AsNoTracking()
            .Include(r => r.Deadline)
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.IsSent)
            .ThenBy(r => r.ReminderDate)
            .ToListAsync(ct);

        return reminders.Select(ToResponse).ToList();
    }

    public async Task<ReminderResponse> GetByIdAsync(
        Guid userId,
        Guid reminderId,
        CancellationToken ct = default)
    {
        var reminder = await _db.Reminders
            .AsNoTracking()
            .Include(r => r.Deadline)
            .FirstOrDefaultAsync(r => r.Id == reminderId && r.UserId == userId, ct);

        if (reminder is null)
            throw new NotFoundException($"Reminder {reminderId} was not found.");

        return ToResponse(reminder);
    }

    public async Task<ReminderResponse> CreateAsync(
        Guid userId,
        CreateReminderRequest request,
        CancellationToken ct = default)
    {
        ValidateChannel(request.Channel);

        var deadline = await _db.Deadlines
            .FirstOrDefaultAsync(d => d.Id == request.DeadlineId && d.UserId == userId, ct);

        if (deadline is null)
            throw new NotFoundException($"Deadline {request.DeadlineId} was not found.");

        ValidateReminderDate(request.ReminderDate, deadline.DueDate);

        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            DeadlineId = deadline.Id,
            UserId = userId,
            ReminderDate = request.ReminderDate,
            Channel = request.Channel,
            IsSent = false,
            SentAt = null,
            CreatedAt = DateTime.UtcNow
        };

        _db.Reminders.Add(reminder);
        await _db.SaveChangesAsync(ct);

        reminder.Deadline = deadline;
        return ToResponse(reminder);
    }

    public async Task<ReminderResponse> UpdateAsync(
        Guid userId,
        Guid reminderId,
        UpdateReminderRequest request,
        CancellationToken ct = default)
    {
        ValidateChannel(request.Channel);

        var reminder = await _db.Reminders
            .Include(r => r.Deadline)
            .FirstOrDefaultAsync(r => r.Id == reminderId && r.UserId == userId, ct);

        if (reminder is null)
            throw new NotFoundException($"Reminder {reminderId} was not found.");

        ValidateReminderDate(request.ReminderDate, reminder.Deadline.DueDate);

        if (reminder.IsSent)
            throw new ConflictException("A sent reminder cannot be modified.");

        reminder.ReminderDate = request.ReminderDate;
        reminder.Channel = request.Channel;

        await _db.SaveChangesAsync(ct);
        return ToResponse(reminder);
    }

    public async Task DeleteAsync(
        Guid userId,
        Guid reminderId,
        CancellationToken ct = default)
    {
        var reminder = await _db.Reminders
            .FirstOrDefaultAsync(r => r.Id == reminderId && r.UserId == userId, ct);

        if (reminder is null)
            throw new NotFoundException($"Reminder {reminderId} was not found.");

        _db.Reminders.Remove(reminder);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Marks due unsent InApp reminders as sent. Email reminders are deliberately
    /// not marked as sent because Phase 6 has no SMTP/email delivery integration.
    /// </summary>
    public async Task<int> ProcessDueInAppRemindersAsync(
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var reminders = await _db.Reminders
            .Where(r => !r.IsSent &&
                        r.Channel == ReminderChannel.InApp &&
                        r.ReminderDate <= now)
            .ToListAsync(ct);

        if (reminders.Count == 0)
            return 0;

        foreach (var reminder in reminders)
        {
            reminder.IsSent = true;
            reminder.SentAt = now;
        }

        await _db.SaveChangesAsync(ct);
        return reminders.Count;
    }

    private static void ValidateChannel(ReminderChannel channel)
    {
        if (!Enum.IsDefined(channel))
            throw new ArgumentException("Invalid reminder channel.", nameof(channel));
    }

    private static void ValidateReminderDate(DateTime reminderDate, DateTime dueDate)
    {
        if (reminderDate > dueDate)
        {
            throw new ConflictException(
                "ReminderDate cannot be later than the deadline DueDate.");
        }
    }

    private static ReminderResponse ToResponse(Reminder reminder) => new()
    {
        Id = reminder.Id,
        DeadlineId = reminder.DeadlineId,
        DeadlineTitle = reminder.Deadline?.Title ?? string.Empty,
        ReminderDate = reminder.ReminderDate,
        Channel = reminder.Channel,
        IsSent = reminder.IsSent,
        SentAt = reminder.SentAt,
        CreatedAt = reminder.CreatedAt
    };
}