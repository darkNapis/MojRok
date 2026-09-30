using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MojRok.Domain.Entities;

namespace MojRok.Infrastructure.Persistence.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReminderDate)
            .IsRequired();

        builder.Property(r => r.Channel)
            .HasConversion<string>()
            .HasMaxLength(20);

        // BackgroundService query: find all unsent reminders due on or before today.
        builder.HasIndex(r => new { r.IsSent, r.ReminderDate });

        // Cascade: deleting a Deadline removes all its Reminders.
        builder.HasOne(r => r.Deadline)
            .WithMany(d => d.Reminders)
            .HasForeignKey(r => r.DeadlineId)
            .OnDelete(DeleteBehavior.Cascade);

        // NoAction on Reminder -> User FK.
        // When a User is deleted the cascade chain is:
        //   User (deleted) -> Deadlines (Cascade) -> Reminders (Cascade via DeadlineId)
        // Reminders are gone before PostgreSQL re-checks reminder.UserId.
        // A second Cascade from Reminder -> User would create multiple cascade
        // paths on the same table, which PostgreSQL rejects with a migration error.
        builder.HasOne(r => r.User)
            .WithMany(u => u.Reminders)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
