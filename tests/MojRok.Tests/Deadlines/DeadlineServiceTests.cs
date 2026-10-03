using Microsoft.EntityFrameworkCore;
using MojRok.Application.Deadlines;
using MojRok.Application.Deadlines.DTOs;
using MojRok.Application.Exceptions;
using MojRok.Domain.Entities;
using MojRok.Infrastructure.Persistence;
using Xunit;

namespace MojRok.Tests.Deadlines;

/// <summary>
/// Integration-style unit tests for DeadlineService using EF Core InMemory.
/// Each test gets a fresh isolated database.
/// Status boundary cases are tested through the full service pipeline
/// (Create/Seed -> GetById -> assert response.Status), which verifies that
/// the computed Status is correctly surfaced in the DTO.
/// </summary>
public class DeadlineServiceTests : IDisposable
{
    private readonly AppDbContext    _db;
    private readonly DeadlineService _service;
    private readonly Guid            _userId      = Guid.NewGuid();
    private readonly Guid            _otherUserId = Guid.NewGuid();

    public DeadlineServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db      = new AppDbContext(options);
        _service = new DeadlineService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ---- Seed helpers -----------------------------------------------

    private async Task<Category> SeedCategoryAsync(Guid? ownerId = null, bool isDefault = false)
    {
        var cat = new Category
        {
            Id        = Guid.NewGuid(),
            Name      = isDefault ? "System Default" : "Personal",
            Color     = "#3B82F6",
            IsDefault = isDefault,
            UserId    = ownerId
        };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return cat;
    }

    private async Task<Deadline> SeedDeadlineAsync(
        Guid?    userId     = null,
        Guid?    categoryId = null,
        DateTime? dueDate   = null,
        bool     isCompleted = false)
    {
        // Each seeded deadline needs a category; create one if not supplied
        var catId = categoryId ?? (await SeedCategoryAsync(userId ?? _userId)).Id;
        var now   = DateTime.UtcNow;

        var d = new Deadline
        {
            Id          = Guid.NewGuid(),
            Title       = "Seeded Deadline",
            DueDate     = dueDate ?? now.AddDays(30),
            UserId      = userId ?? _userId,
            CategoryId  = catId,
            IsCompleted = isCompleted,
            CompletedAt = isCompleted ? now : null,
            CreatedAt   = now,
            UpdatedAt   = now
        };

        _db.Deadlines.Add(d);
        await _db.SaveChangesAsync();
        return d;
    }

    // ================================================================
    // CREATE
    // ================================================================

    [Fact]
    public async Task Create_ReturnsDeadlineWithCorrectTitle()
    {
        var cat    = await SeedCategoryAsync(_userId);
        var result = await _service.CreateAsync(_userId, new CreateDeadlineRequest
        {
            Title      = "Pay taxes",
            DueDate    = DateTime.UtcNow.AddDays(10),
            CategoryId = cat.Id
        });

        Assert.Equal("Pay taxes", result.Title);
        Assert.Equal(cat.Id,      result.CategoryId);
    }

    [Fact]
    public async Task Create_SetsUserIdFromParameter()
    {
        var cat = await SeedCategoryAsync(_userId);
        await _service.CreateAsync(_userId, new CreateDeadlineRequest
        {
            Title      = "Test",
            DueDate    = DateTime.UtcNow.AddDays(5),
            CategoryId = cat.Id
        });

        var stored = await _db.Deadlines.FirstAsync();
        Assert.Equal(_userId, stored.UserId);
    }

    [Fact]
    public async Task Create_StartsAsNotCompleted()
    {
        var cat    = await SeedCategoryAsync(_userId);
        var result = await _service.CreateAsync(_userId, new CreateDeadlineRequest
        {
            Title      = "Test",
            DueDate    = DateTime.UtcNow.AddDays(5),
            CategoryId = cat.Id
        });

        Assert.False(result.IsCompleted);
        Assert.Null(result.CompletedAt);
    }

    [Fact]
    public async Task Create_ThrowsNotFound_WhenCategoryDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(_userId, new CreateDeadlineRequest
            {
                Title      = "Test",
                DueDate    = DateTime.UtcNow.AddDays(5),
                CategoryId = Guid.NewGuid()   // non-existent
            }));
    }

    [Fact]
    public async Task Create_ThrowsNotFound_WhenCategoryBelongsToAnotherUser()
    {
        var otherCat = await SeedCategoryAsync(_otherUserId);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(_userId, new CreateDeadlineRequest
            {
                Title      = "Test",
                DueDate    = DateTime.UtcNow.AddDays(5),
                CategoryId = otherCat.Id
            }));
    }

    [Fact]
    public async Task Create_AllowsSystemDefaultCategory()
    {
        var defaultCat = await SeedCategoryAsync(ownerId: null, isDefault: true);

        var result = await _service.CreateAsync(_userId, new CreateDeadlineRequest
        {
            Title      = "Tax deadline",
            DueDate    = DateTime.UtcNow.AddDays(30),
            CategoryId = defaultCat.Id
        });

        Assert.Equal(defaultCat.Id, result.CategoryId);
    }

    [Fact]
    public async Task Create_ThrowsArgumentException_WhenRecurringWithoutInterval()
    {
        var cat = await SeedCategoryAsync(_userId);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(_userId, new CreateDeadlineRequest
            {
                Title                  = "Monthly Bill",
                DueDate                = DateTime.UtcNow.AddDays(10),
                CategoryId             = cat.Id,
                IsRecurring            = true,
                RecurrenceIntervalDays = null   // missing
            }));
    }

    // ================================================================
    // GET ALL
    // ================================================================

    [Fact]
    public async Task GetAll_ReturnsOnlyCurrentUsersDeadlines()
    {
        var cat = await SeedCategoryAsync(_userId);
        await SeedDeadlineAsync(_userId,      cat.Id);
        await SeedDeadlineAsync(_userId,      cat.Id);
        await SeedDeadlineAsync(_otherUserId);   // different user

        var result = await _service.GetAllAsync(_userId);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.NotEqual(_otherUserId.ToString(), r.Title));
    }

    [Fact]
    public async Task GetAll_DoesNotReturnOtherUsersDeadlines()
    {
        await SeedDeadlineAsync(_otherUserId);

        var result = await _service.GetAllAsync(_userId);

        Assert.Empty(result);
    }

    // ================================================================
    // GET BY ID
    // ================================================================

    [Fact]
    public async Task GetById_ReturnsOwnDeadline()
    {
        var d      = await SeedDeadlineAsync(_userId);
        var result = await _service.GetByIdAsync(_userId, d.Id);

        Assert.Equal(d.Id, result.Id);
    }

    [Fact]
    public async Task GetById_ThrowsNotFound_WhenMissing()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetByIdAsync(_userId, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetById_ThrowsNotFound_WhenBelongsToAnotherUser()
    {
        var d = await SeedDeadlineAsync(_otherUserId);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetByIdAsync(_userId, d.Id));
    }

    // ================================================================
    // UPDATE
    // ================================================================

    [Fact]
    public async Task Update_UpdatesAllowedFields()
    {
        var cat  = await SeedCategoryAsync(_userId);
        var d    = await SeedDeadlineAsync(_userId, cat.Id);

        // create a second category the normal way
        var cat2 = new Category
        {
            Id        = Guid.NewGuid(),
            Name      = "Category 2",
            Color     = "#10B981",
            IsDefault = false,
            UserId    = _userId
        };
        _db.Categories.Add(cat2);
        await _db.SaveChangesAsync();

        var result = await _service.UpdateAsync(_userId, d.Id, new UpdateDeadlineRequest
        {
            Title      = "Updated Title",
            DueDate    = DateTime.UtcNow.AddDays(20),
            CategoryId = cat2.Id
        });

        Assert.Equal("Updated Title", result.Title);
        Assert.Equal(cat2.Id,         result.CategoryId);
    }

    [Fact]
    public async Task Update_ThrowsNotFound_WhenBelongsToAnotherUser()
    {
        var cat = await SeedCategoryAsync(_userId);
        var d   = await SeedDeadlineAsync(_otherUserId);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateAsync(_userId, d.Id, new UpdateDeadlineRequest
            {
                Title      = "Hack",
                DueDate    = DateTime.UtcNow.AddDays(5),
                CategoryId = cat.Id
            }));
    }

    [Fact]
    public async Task Update_DoesNotChangeProtectedFields()
    {
        var cat    = await SeedCategoryAsync(_userId);
        var d      = await SeedDeadlineAsync(_userId, cat.Id);
        var origCreatedAt = d.CreatedAt;

        await _service.UpdateAsync(_userId, d.Id, new UpdateDeadlineRequest
        {
            Title      = "New Title",
            DueDate    = DateTime.UtcNow.AddDays(5),
            CategoryId = cat.Id
        });

        var stored = await _db.Deadlines.FirstAsync(x => x.Id == d.Id);
        Assert.Equal(_userId,        stored.UserId);
        Assert.Equal(origCreatedAt,  stored.CreatedAt);
        Assert.False(stored.IsCompleted);
    }

    // ================================================================
    // DELETE
    // ================================================================

    [Fact]
    public async Task Delete_RemovesOwnDeadline()
    {
        var d = await SeedDeadlineAsync(_userId);

        await _service.DeleteAsync(_userId, d.Id);

        var exists = await _db.Deadlines.AnyAsync(x => x.Id == d.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task Delete_ThrowsNotFound_WhenBelongsToAnotherUser()
    {
        var d = await SeedDeadlineAsync(_otherUserId);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.DeleteAsync(_userId, d.Id));
    }

    // ================================================================
    // COMPLETE
    // ================================================================

    [Fact]
    public async Task Complete_SetsIsCompletedAndCompletedAt()
    {
        var d      = await SeedDeadlineAsync(_userId);
        var result = await _service.CompleteAsync(_userId, d.Id);

        Assert.True(result.IsCompleted);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task Complete_StatusBecomesCompleted()
    {
        var d      = await SeedDeadlineAsync(_userId);
        var result = await _service.CompleteAsync(_userId, d.Id);

        Assert.Equal("Completed", result.Status);
    }

    [Fact]
    public async Task Complete_IsIdempotent_WhenAlreadyCompleted()
    {
        var d = await SeedDeadlineAsync(_userId, isCompleted: true);

        // Should not throw, should return current state
        var result = await _service.CompleteAsync(_userId, d.Id);

        Assert.True(result.IsCompleted);
    }

    // ================================================================
    // STATUS - tested through the full service response pipeline
    // (verifies that computed Status is correctly surfaced in the DTO)
    // ================================================================

    [Theory]
    [InlineData(-1, "Expired")]
    [InlineData(0,  "Urgent")]
    [InlineData(3,  "Urgent")]
    [InlineData(4,  "Upcoming")]
    [InlineData(14, "Upcoming")]
    [InlineData(15, "Active")]
    [InlineData(90, "Active")]
    public async Task GetById_ReturnsCorrectStatus(int daysFromToday, string expectedStatus)
    {
        var dueDate = DateTime.UtcNow.Date.AddDays(daysFromToday);
        var d       = await SeedDeadlineAsync(_userId, dueDate: dueDate);

        var result  = await _service.GetByIdAsync(_userId, d.Id);

        Assert.Equal(expectedStatus, result.Status);
    }

    [Fact]
    public async Task GetById_Status_IsCompleted_WhenIsCompletedTrue_EvenIfPastDue()
    {
        var d      = await SeedDeadlineAsync(_userId, dueDate: DateTime.UtcNow.AddDays(-30), isCompleted: true);
        var result = await _service.GetByIdAsync(_userId, d.Id);

        Assert.Equal("Completed", result.Status);
    }
}

// Extension helper so tests can pass an optional category name
file static class CategoryExtensions
{
    public static async Task<Category> SeedCategoryAsync(
        this Microsoft.EntityFrameworkCore.DbContext db,
        Guid? ownerId = null,
        bool isDefault = false,
        string name = "Personal")
    {
        var cat = new Category
        {
            Id        = Guid.NewGuid(),
            Name      = name,
            Color     = "#3B82F6",
            IsDefault = isDefault,
            UserId    = ownerId
        };
        db.Set<Category>().Add(cat);
        await db.SaveChangesAsync();
        return cat;
    }
}
