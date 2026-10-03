using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MojRok.Application.Exceptions;
using MojRok.Application.Reminders;
using MojRok.Application.Reminders.DTOs;
using MojRok.Domain.Entities;
using MojRok.Domain.Enums;
using MojRok.Infrastructure.Persistence;
using Xunit;

namespace MojRok.Tests.Reminders;

public class ReminderServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ReminderService _service;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    public ReminderServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new ReminderService(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Category> SeedCategoryAsync(Guid ownerId)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Color = "#3B82F6",
            UserId = ownerId,
            IsDefault = false
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    private async Task<Deadline> SeedDeadlineAsync(
        Guid? userId = null,
        DateTime? dueDate = null)
    {
        var owner = userId ?? _userId;
        var category = await SeedCategoryAsync(owner);
        var deadline = new Deadline
        {
            Id = Guid.NewGuid(),
            Title = "Test deadline",
            DueDate = dueDate ?? DateTime.UtcNow.AddDays(10),
            UserId = owner,
            CategoryId = category.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Deadlines.Add(deadline);
        await _db.SaveChangesAsync();
        return deadline;
    }

    [Fact]
    public async Task Create_CreatesOwnReminder()
    {
        var deadline = await SeedDeadlineAsync();
        var result = await _service.CreateAsync(_userId, new CreateReminderRequest
        {
            DeadlineId = deadline.Id,
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Channel = ReminderChannel.InApp
        });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(deadline.Id, result.DeadlineId);
        Assert.Equal("Test deadline", result.DeadlineTitle);
        Assert.False(result.IsSent);
        Assert.Null(result.SentAt);
    }

    [Fact]
    public async Task Create_RejectsOtherUsersDeadline()
    {
        var deadline = await SeedDeadlineAsync(_otherUserId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateAsync(_userId, new CreateReminderRequest
            {
                DeadlineId = deadline.Id,
                ReminderDate = DateTime.UtcNow.AddDays(1)
            }));
    }

    [Fact]
    public async Task Create_RejectsReminderAfterDeadline()
    {
        var deadline = await SeedDeadlineAsync(_userId, DateTime.UtcNow.AddDays(2));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CreateAsync(_userId, new CreateReminderRequest
            {
                DeadlineId = deadline.Id,
                ReminderDate = DateTime.UtcNow.AddDays(3)
            }));
    }

    [Fact]
    public async Task Create_RejectsInvalidChannel()
    {
        var deadline = await SeedDeadlineAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync(_userId, new CreateReminderRequest
            {
                DeadlineId = deadline.Id,
                ReminderDate = DateTime.UtcNow.AddDays(1),
                Channel = (ReminderChannel)999
            }));
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyCurrentUsersReminders()
    {
        var ownDeadline = await SeedDeadlineAsync(_userId);
        var otherDeadline = await SeedDeadlineAsync(_otherUserId);

        await _service.CreateAsync(_userId, new CreateReminderRequest
        {
            DeadlineId = ownDeadline.Id,
            ReminderDate = DateTime.UtcNow.AddDays(1)
        });
        _db.Reminders.Add(new Reminder
        {
            Id = Guid.NewGuid(),
            DeadlineId = otherDeadline.Id,
            UserId = _otherUserId,
            ReminderDate = DateTime.UtcNow.AddDays(1),
            Deadline = otherDeadline
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync(_userId);
        Assert.Single(result);
        Assert.Equal(ownDeadline.Id, result[0].DeadlineId);
    }

    [Fact]
    public async Task GetById_CannotReturnOtherUsersReminder()
    {
        var deadline = await SeedDeadlineAsync(_otherUserId);
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            DeadlineId = deadline.Id,
            UserId = _otherUserId,
            ReminderDate = DateTime.UtcNow.AddDays(1)
        };
        _db.Reminders.Add(reminder);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetByIdAsync(_userId, reminder.Id));
    }

    [Fact]
    public async Task GetById_ReturnsOwnReminder()
    {
        var deadline = await SeedDeadlineAsync();
        var created = await _service.CreateAsync(_userId, new CreateReminderRequest
        {
            DeadlineId = deadline.Id,
            ReminderDate = DateTime.UtcNow.AddDays(1)
        });

        var result = await _service.GetByIdAsync(_userId, created.Id);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal(deadline.Title, result.DeadlineTitle);
    }

    [Fact]
    public async Task Update_ChangesDateAndChannel()
    {
        var deadline = await SeedDeadlineAsync();
        var created = await _service.CreateAsync(_userId, new CreateReminderRequest
        {
            DeadlineId = deadline.Id,
            ReminderDate = DateTime.UtcNow.AddDays(1)
        });

        var updated = await _service.UpdateAsync(_userId, created.Id, new UpdateReminderRequest
        {
            ReminderDate = DateTime.UtcNow.AddDays(2),
            Channel = ReminderChannel.Email
        });

        Assert.Equal(ReminderChannel.Email, updated.Channel);
        Assert.True(updated.ReminderDate > created.ReminderDate);
    }

    [Fact]
    public async Task Update_CannotUpdateOtherUsersReminder()
    {
        var deadline = await SeedDeadlineAsync(_otherUserId);
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            DeadlineId = deadline.Id,
            UserId = _otherUserId,
            ReminderDate = DateTime.UtcNow.AddDays(1)
        };
        _db.Reminders.Add(reminder);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UpdateAsync(_userId, reminder.Id, new UpdateReminderRequest
            {
                ReminderDate = DateTime.UtcNow.AddDays(2)
            }));
    }

    [Fact]
    public async Task Update_DoesNotAllowChangingProtectedFields()
    {
        var deadline = await SeedDeadlineAsync();
        var created = await _service.CreateAsync(_userId, new CreateReminderRequest
        {
            DeadlineId = deadline.Id,
            ReminderDate = DateTime.UtcNow.AddDays(1)
        });

        var before = await _db.Reminders.FirstAsync(r => r.Id == created.Id);
        var createdAt = before.CreatedAt;
        var deadlineId = before.DeadlineId;
        var userId = before.UserId;

        await _service.UpdateAsync(_userId, created.Id, new UpdateReminderRequest
        {
            ReminderDate = DateTime.UtcNow.AddDays(2),
            Channel = ReminderChannel.Email
        });

        var after = await _db.Reminders.FirstAsync(r => r.Id == created.Id);
        Assert.Equal(deadlineId, after.DeadlineId);
        Assert.Equal(userId, after.UserId);
        Assert.Equal(createdAt, after.CreatedAt);
    }

    [Fact]
    public async Task Delete_DeletesOwnReminder()
    {
        var deadline = await SeedDeadlineAsync();
        var created = await _service.CreateAsync(_userId, new CreateReminderRequest
        {
            DeadlineId = deadline.Id,
            ReminderDate = DateTime.UtcNow.AddDays(1)
        });

        await _service.DeleteAsync(_userId, created.Id);
        Assert.False(await _db.Reminders.AnyAsync(r => r.Id == created.Id));
    }

    [Fact]
    public async Task Delete_CannotDeleteOtherUsersReminder()
    {
        var deadline = await SeedDeadlineAsync(_otherUserId);
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            DeadlineId = deadline.Id,
            UserId = _otherUserId,
            ReminderDate = DateTime.UtcNow.AddDays(1)
        };
        _db.Reminders.Add(reminder);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.DeleteAsync(_userId, reminder.Id));
    }

    [Fact]
    public async Task BackgroundProcessor_MarksDueInAppReminderAsSent()
    {
        var deadline = await SeedDeadlineAsync();
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            DeadlineId = deadline.Id,
            UserId = _userId,
            ReminderDate = DateTime.UtcNow.AddMinutes(-1),
            Channel = ReminderChannel.InApp,
            IsSent = false
        };
        _db.Reminders.Add(reminder);
        await _db.SaveChangesAsync();

        var count = await _service.ProcessDueInAppRemindersAsync();
        var stored = await _db.Reminders.FirstAsync(r => r.Id == reminder.Id);

        Assert.Equal(1, count);
        Assert.True(stored.IsSent);
        Assert.NotNull(stored.SentAt);
    }

    [Fact]
    public async Task BackgroundProcessor_DoesNotProcessFutureReminder()
    {
        var deadline = await SeedDeadlineAsync();
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            DeadlineId = deadline.Id,
            UserId = _userId,
            ReminderDate = DateTime.UtcNow.AddHours(1),
            Channel = ReminderChannel.InApp,
            IsSent = false
        };
        _db.Reminders.Add(reminder);
        await _db.SaveChangesAsync();

        var count = await _service.ProcessDueInAppRemindersAsync();
        var stored = await _db.Reminders.FirstAsync(r => r.Id == reminder.Id);

        Assert.Equal(0, count);
        Assert.False(stored.IsSent);
        Assert.Null(stored.SentAt);
    }

    [Fact]
    public async Task BackgroundProcessor_DoesNotMarkEmailAsSent()
    {
        var deadline = await SeedDeadlineAsync();
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            DeadlineId = deadline.Id,
            UserId = _userId,
            ReminderDate = DateTime.UtcNow.AddMinutes(-1),
            Channel = ReminderChannel.Email,
            IsSent = false
        };
        _db.Reminders.Add(reminder);
        await _db.SaveChangesAsync();

        var count = await _service.ProcessDueInAppRemindersAsync();
        var stored = await _db.Reminders.FirstAsync(r => r.Id == reminder.Id);

        Assert.Equal(0, count);
        Assert.False(stored.IsSent);
        Assert.Null(stored.SentAt);
    }

    [Fact]
    public async Task BackgroundProcessor_IsIdempotentForAlreadySentReminder()
    {
        var deadline = await SeedDeadlineAsync();
        var sentAt = DateTime.UtcNow.AddMinutes(-5);
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            DeadlineId = deadline.Id,
            UserId = _userId,
            ReminderDate = DateTime.UtcNow.AddMinutes(-10),
            Channel = ReminderChannel.InApp,
            IsSent = true,
            SentAt = sentAt
        };
        _db.Reminders.Add(reminder);
        await _db.SaveChangesAsync();

        var count = await _service.ProcessDueInAppRemindersAsync();
        var stored = await _db.Reminders.FirstAsync(r => r.Id == reminder.Id);

        Assert.Equal(0, count);
        Assert.Equal(sentAt, stored.SentAt);
    }
}