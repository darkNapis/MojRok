using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MojRok.Domain.Entities;

namespace MojRok.Infrastructure.Persistence.Configurations;

public class DeadlineConfiguration : IEntityTypeConfiguration<Deadline>
{
    public void Configure(EntityTypeBuilder<Deadline> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Description)
            .HasMaxLength(1000);

        builder.Property(d => d.DueDate)
            .IsRequired();

        // Status is computed from DueDate at runtime. Never stored in the DB.
        builder.Ignore(d => d.Status);

        // Composite indexes to support the most common queries:
        //   - All deadlines for a user
        //   - Filter/sort by due date per user
        //   - Filter by completion state per user
        builder.HasIndex(d => d.UserId);
        builder.HasIndex(d => new { d.UserId, d.DueDate });
        builder.HasIndex(d => new { d.UserId, d.IsCompleted });

        // Cascade: deleting a user removes all their deadlines.
        builder.HasOne(d => d.User)
            .WithMany(u => u.Deadlines)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: prevents deleting a Category that still has Deadlines.
        // The API must enforce category change/re-assignment before deletion.
        builder.HasOne(d => d.Category)
            .WithMany(c => c.Deadlines)
            .HasForeignKey(d => d.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
