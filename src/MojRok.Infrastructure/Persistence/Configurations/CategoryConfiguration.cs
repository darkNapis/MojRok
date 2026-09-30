using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MojRok.Domain.Entities;

namespace MojRok.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Color)
            .IsRequired()
            .HasMaxLength(7)
            .HasDefaultValue("#6366F1");

        builder.Property(c => c.IconSlug)
            .HasMaxLength(50);

        // UserId is NULLABLE (Guid?).
        // null  = system/default category visible to all users.
        // value = personal category belonging to a specific user.
        // IsRequired(false) explicitly maps the nullable FK column.
        // Cascade: deleting a user removes their personal categories.
        //          System categories (UserId = null) are not affected.
        builder.HasOne(c => c.User)
            .WithMany(u => u.Categories)
            .HasForeignKey(c => c.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
