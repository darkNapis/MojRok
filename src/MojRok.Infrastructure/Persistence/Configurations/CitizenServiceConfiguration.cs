using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MojRok.Domain.Entities;

namespace MojRok.Infrastructure.Persistence.Configurations;

public class CitizenServiceConfiguration : IEntityTypeConfiguration<CitizenService>
{
    public void Configure(EntityTypeBuilder<CitizenService> builder)
    {
        builder.HasKey(cs => cs.Id);

        builder.Property(cs => cs.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cs => cs.NameMk)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cs => cs.Description)
            .HasMaxLength(1000);

        builder.Property(cs => cs.DescriptionMk)
            .HasMaxLength(1000);

        builder.Property(cs => cs.WebsiteUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(cs => cs.PhoneNumber)
            .HasMaxLength(20);

        builder.HasIndex(cs => cs.ServiceCategoryId);
        builder.HasIndex(cs => cs.IsActive);

        // Restrict: prevents deleting a ServiceCategory that still has services.
        builder.HasOne(cs => cs.ServiceCategory)
            .WithMany(sc => sc.CitizenServices)
            .HasForeignKey(cs => cs.ServiceCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // MunicipalityId nullable: null means the service is national (not city-specific).
        // SetNull: deleting a municipality clears the FK, keeps the service record.
        builder.HasOne(cs => cs.Municipality)
            .WithMany(m => m.CitizenServices)
            .HasForeignKey(cs => cs.MunicipalityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
