using Microsoft.EntityFrameworkCore;
using MojRok.Domain.Entities;

namespace MojRok.Infrastructure.Persistence;

/// <summary>
/// Seeds reference data that must exist before the application serves traffic.
/// Called once at startup. Safe to call multiple times (idempotent).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedDefaultCategoriesAsync(AppDbContext db)
    {
        // Skip if defaults already exist
        if (await db.Categories.AnyAsync(c => c.IsDefault))
            return;

        var defaults = new Category[]
        {
            new() { Id = Guid.NewGuid(), Name = "Taxes",      Color = "#EF4444", IconSlug = "receipt-tax",  IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Documents",  Color = "#3B82F6", IconSlug = "document",     IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Health",     Color = "#10B981", IconSlug = "heart",        IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Vehicle",    Color = "#F59E0B", IconSlug = "truck",        IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Utilities",  Color = "#8B5CF6", IconSlug = "bolt",         IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Education",  Color = "#06B6D4", IconSlug = "academic-cap", IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Legal",      Color = "#64748B", IconSlug = "scale",        IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Other",      Color = "#6366F1", IconSlug = "tag",          IsDefault = true },
        };

        db.Categories.AddRange(defaults);
        await db.SaveChangesAsync();
    }
}
