using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MojRok.Domain.Entities;

namespace MojRok.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(u => u.PreferredLanguage)
            .IsRequired()
            .HasMaxLength(2)
            .HasDefaultValue("mk");

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        // MunicipalityId is nullable: a user may not belong to any municipality.
        // SetNull: deleting a municipality clears user.MunicipalityId, never deletes the user.
        builder.HasOne(u => u.Municipality)
            .WithMany(m => m.Users)
            .HasForeignKey(u => u.MunicipalityId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
