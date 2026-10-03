using Microsoft.EntityFrameworkCore;
using MojRok.Application.Dashboard;
using MojRok.Domain.Entities;
using MojRok.Infrastructure.Persistence;
using Xunit;

namespace MojRok.Tests.Dashboard;

public class DashboardServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly DashboardService _service;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _service = new DashboardService(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Category> SeedCategoryAsync(Guid ownerId)
    {
        var cat = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Color = "#3B82F6",
            UserId = ownerId,
            IsDefault = false
        };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return cat;
    }

    private async Task SeedDeadlineAsync(
        Guid userId,
        DateTime dueDate,
        bool isCompleted = false,
        DateTime? completedAt = null)
    {
        var cat = await SeedCategoryAsync(userId);
        var now = DateTime.UtcNow;
        _db.Deadlines.Add(new Deadline
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            DueDate = dueDate,
            UserId = userId,
            CategoryId = cat.Id,
            IsCompleted = isCompleted,
            CompletedAt = completedAt,
            CreatedAt = now,
            UpdatedAt = now
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Get_ReturnsOnlyCurrentUsersData()
    {
        await SeedDeadlineAsync(_userId, DateTime.UtcNow.AddDays(1));
        await SeedDeadlineAsync(_otherUserId, DateTime.UtcNow.AddDays(1));

        var result = await _service.GetAsync(_userId);

        Assert.Equal(1, result.TotalByStatus.Values.Sum());
    }

    [Fact]
    public async Task Get_CountsOverdueCorrectly()
    {
        await SeedDeadlineAsync(_userId, DateTime.UtcNow.AddDays(-2)); // overdue
        await SeedDeadlineAsync(_userId, DateTime.UtcNow.AddDays(5));  // not overdue

        var result = await _service.GetAsync(_userId);

        Assert.Equal(1, result.OverdueCount);
    }

    [Fact]
    public async Task Get_CountsUpcomingNext7Days()
    {
        await SeedDeadlineAsync(_userId, DateTime.UtcNow.AddDays(3));  // inside 7 days
        await SeedDeadlineAsync(_userId, DateTime.UtcNow.AddDays(10)); // outside

        var result = await _service.GetAsync(_userId);

        Assert.Equal(1, result.UpcomingNext7Days);
    }

    [Fact]
    public async Task Get_CountsRecentlyCompleted()
    {
        await SeedDeadlineAsync(_userId, DateTime.UtcNow.AddDays(-1),
            isCompleted: true, completedAt: DateTime.UtcNow.AddDays(-2));

        var result = await _service.GetAsync(_userId);

        Assert.Equal(1, result.RecentlyCompleted);
    }

    [Fact]
    public async Task Get_ReturnsMaxThreeUrgent()
    {
        // Create 5 deadlines that should be Urgent (due today / within 3 days)
        for (int i = 0; i < 5; i++)
            await SeedDeadlineAsync(_userId, DateTime.UtcNow.Date.AddDays(i % 3));

        var result = await _service.GetAsync(_userId);

        Assert.True(result.Next3Urgent.Count <= 3);
    }
}