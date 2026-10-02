using Microsoft.EntityFrameworkCore;
using MojRok.Application.Abstractions;
using MojRok.Application.Categories.DTOs;
using MojRok.Application.Exceptions;
using MojRok.Domain.Entities;

namespace MojRok.Application.Categories;

public class CategoryService
{
    private readonly IAppDbContext _db;

    public CategoryService(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns system default categories (UserId = null)
    /// plus the current user's personal categories.
    /// Never returns another user's categories.
    /// </summary>
    public async Task<List<CategoryResponse>> GetCategoriesAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var categories = await _db.Categories
            .Where(c => c.UserId == null || c.UserId == userId)
            .OrderBy(c => c.IsDefault ? 0 : 1)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

        return categories.Select(ToResponse).ToList();
    }

    /// <summary>
    /// Creates a personal category for the current user.
    /// UserId is always taken from the JWT â€” never from the request.
    /// IsDefault is always false for user-created categories.
    /// </summary>
    public async Task<CategoryResponse> CreateCategoryAsync(
        Guid userId,
        CreateCategoryRequest request,
        CancellationToken ct = default)
    {
        var category = new Category
        {
            Id        = Guid.NewGuid(),
            Name      = request.Name.Trim(),
            Color     = request.Color.Trim(),
            IconSlug  = request.IconSlug?.Trim(),
            UserId    = userId,
            IsDefault = false   // user-created categories are never defaults
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);
        return ToResponse(category);
    }

    /// <summary>
    /// Updates a personal category.
    /// Forbidden if: category belongs to another user, or is a system default.
    /// </summary>
    public async Task<CategoryResponse> UpdateCategoryAsync(
        Guid userId,
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken ct = default)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

        if (category is null)
            throw new NotFoundException($"Category {categoryId} was not found.");

        if (category.IsDefault)
            throw new ForbiddenException("System default categories cannot be modified.");

        if (category.UserId != userId)
            throw new ForbiddenException("You can only modify your own categories.");

        category.Name     = request.Name.Trim();
        category.Color    = request.Color.Trim();
        category.IconSlug = request.IconSlug?.Trim();

        await _db.SaveChangesAsync(ct);
        return ToResponse(category);
    }

    /// <summary>
    /// Deletes a personal category.
    /// Forbidden if: category belongs to another user, or is a system default.
    /// Conflict if: category still has Deadlines assigned to it.
    /// </summary>
    public async Task DeleteCategoryAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken ct = default)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

        if (category is null)
            throw new NotFoundException($"Category {categoryId} was not found.");

        if (category.IsDefault)
            throw new ForbiddenException("System default categories cannot be deleted.");

        if (category.UserId != userId)
            throw new ForbiddenException("You can only delete your own categories.");

        // Check for assigned deadlines without loading them into memory
        var hasDeadlines = await _db.Deadlines
            .AnyAsync(d => d.CategoryId == categoryId, ct);

        if (hasDeadlines)
            throw new ConflictException(
                "This category cannot be deleted because it has deadlines assigned to it. " +
                "Reassign or delete those deadlines first.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);
    }

    private static CategoryResponse ToResponse(Category c) => new()
    {
        Id        = c.Id,
        Name      = c.Name,
        Color     = c.Color,
        IconSlug  = c.IconSlug,
        IsDefault = c.IsDefault,
        UserId    = c.UserId
    };
}
