using Microsoft.EntityFrameworkCore;
using MojRok.Application.Categories;
using MojRok.Application.Categories.DTOs;
using MojRok.Application.Exceptions;
using MojRok.Domain.Entities;
using MojRok.Domain.Enums;
using MojRok.Infrastructure.Persistence;
using Xunit;

namespace MojRok.Tests.Categories;

/// <summary>
/// Unit tests for CategoryService using EF Core InMemory provider.
/// Each test gets a fresh isolated database to prevent state leakage.
/// </summary>
public class CategoryServiceTests : IDisposable
{
    private readonly AppDbContext    _db;
    private readonly CategoryService _service;
    private readonly Guid            _userId    = Guid.NewGuid();
    private readonly Guid            _otherUser = Guid.NewGuid();

    public CategoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db      = new AppDbContext(options);
        _service = new CategoryService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ---- Seed helpers -----------------------------------------------

    private async Task<Category> SeedDefaultAsync(string name = "Taxes")
    {
        var c = new Category
        {
            Id        = Guid.NewGuid(),
            Name      = name,
            Color     = "#EF4444",
            IsDefault = true,
            UserId    = null
        };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();
        return c;
    }

    private async Task<Category> SeedUserCategoryAsync(
        Guid? userId = null,
        string name  = "My Category")
    {
        var c = new Category
        {
            Id        = Guid.NewGuid(),
            Name      = name,
            Color     = "#3B82F6",
            IsDefault = false,
            UserId    = userId ?? _userId
        };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();
        return c;
    }

    private async Task SeedDeadlineForCategoryAsync(Guid categoryId)
    {
        _db.Deadlines.Add(new Deadline
        {
            Id         = Guid.NewGuid(),
            Title      = "Test Deadline",
            DueDate    = DateTime.UtcNow.AddDays(10),
            UserId     = _userId,
            CategoryId = categoryId,
            CreatedAt  = DateTime.UtcNow,
            UpdatedAt  = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    // ================================================================
    // GET /api/categories
    // ================================================================

    [Fact]
    public async Task GetCategories_ReturnsDefaultCategories()
    {
        await SeedDefaultAsync("Taxes");
        await SeedDefaultAsync("Health");

        var result = await _service.GetCategoriesAsync(_userId);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(r.IsDefault));
    }

    [Fact]
    public async Task GetCategories_ReturnsCurrentUsersCategories()
    {
        await SeedDefaultAsync();
        await SeedUserCategoryAsync(_userId, "Personal");

        var result = await _service.GetCategoriesAsync(_userId);

        Assert.Contains(result, r => r.Name == "Personal");
    }

    [Fact]
    public async Task GetCategories_DoesNotReturnOtherUsersCategories()
    {
        await SeedUserCategoryAsync(_userId,    "Mine");
        await SeedUserCategoryAsync(_otherUser, "Theirs");

        var result = await _service.GetCategoriesAsync(_userId);

        Assert.Contains(result,    r => r.Name == "Mine");
        Assert.DoesNotContain(result, r => r.Name == "Theirs");
    }

    [Fact]
    public async Task GetCategories_DefaultsAppearBeforePersonal()
    {
        await SeedUserCategoryAsync(_userId, "ZZZ Personal");
        await SeedDefaultAsync("AAA Default");

        var result = await _service.GetCategoriesAsync(_userId);

        Assert.True(result[0].IsDefault);
        Assert.False(result[1].IsDefault);
    }

    // ================================================================
    // POST /api/categories
    // ================================================================

    [Fact]
    public async Task Create_SetsUserIdFromParameter()
    {
        var result = await _service.CreateCategoryAsync(_userId,
            new CreateCategoryRequest { Name = "Work", Color = "#111111" });

        Assert.Equal(_userId, result.UserId);
    }

    [Fact]
    public async Task Create_IsDefaultAlwaysFalse()
    {
        var result = await _service.CreateCategoryAsync(_userId,
            new CreateCategoryRequest { Name = "Work", Color = "#111111" });

        Assert.False(result.IsDefault);
    }

    [Fact]
    public async Task Create_TrimsName()
    {
        var result = await _service.CreateCategoryAsync(_userId,
            new CreateCategoryRequest { Name = "  Work  ", Color = "#111111" });

        Assert.Equal("Work", result.Name);
    }

    // ================================================================
    // PUT /api/categories/{id}
    // ================================================================

    [Fact]
    public async Task Update_UpdatesOwnCategory()
    {
        var cat = await SeedUserCategoryAsync(_userId);

        var result = await _service.UpdateCategoryAsync(_userId, cat.Id,
            new UpdateCategoryRequest { Name = "Updated", Color = "#FFFFFF" });

        Assert.Equal("Updated", result.Name);
        Assert.Equal("#FFFFFF", result.Color);
    }

    [Fact]
    public async Task Update_ThrowsForbidden_ForDefaultCategory()
    {
        var def = await SeedDefaultAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UpdateCategoryAsync(_userId, def.Id,
                new UpdateCategoryRequest { Name = "Hack", Color = "#000000" }));
    }

    [Fact]
    public async Task Update_ThrowsForbidden_ForOtherUsersCategory()
    {
        var other = await SeedUserCategoryAsync(_otherUser);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UpdateCategoryAsync(_userId, other.Id,
                new UpdateCategoryRequest { Name = "Hack", Color = "#000000" }));
    }

    [Fact]
    public async Task Update_ThrowsNotFound_WhenCategoryMissing()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateCategoryAsync(_userId, Guid.NewGuid(),
                new UpdateCategoryRequest { Name = "X", Color = "#000000" }));
    }

    // ================================================================
    // DELETE /api/categories/{id}
    // ================================================================

    [Fact]
    public async Task Delete_RemovesOwnCategory()
    {
        var cat = await SeedUserCategoryAsync(_userId);

        await _service.DeleteCategoryAsync(_userId, cat.Id);

        var exists = await _db.Categories.AnyAsync(c => c.Id == cat.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task Delete_ThrowsForbidden_ForDefaultCategory()
    {
        var def = await SeedDefaultAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.DeleteCategoryAsync(_userId, def.Id));
    }

    [Fact]
    public async Task Delete_ThrowsForbidden_ForOtherUsersCategory()
    {
        var other = await SeedUserCategoryAsync(_otherUser);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.DeleteCategoryAsync(_userId, other.Id));
    }

    [Fact]
    public async Task Delete_ThrowsConflict_WhenCategoryHasDeadlines()
    {
        var cat = await SeedUserCategoryAsync(_userId);
        await SeedDeadlineForCategoryAsync(cat.Id);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.DeleteCategoryAsync(_userId, cat.Id));
    }

    [Fact]
    public async Task Delete_ThrowsNotFound_WhenCategoryMissing()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.DeleteCategoryAsync(_userId, Guid.NewGuid()));
    }
}
