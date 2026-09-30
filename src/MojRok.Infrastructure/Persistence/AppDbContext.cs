using Microsoft.EntityFrameworkCore;
using MojRok.Application.Abstractions;
using MojRok.Domain.Entities;

namespace MojRok.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser>         Users             => Set<AppUser>();
    public DbSet<Municipality>    Municipalities    => Set<Municipality>();
    public DbSet<Category>        Categories        => Set<Category>();
    public DbSet<Deadline>        Deadlines         => Set<Deadline>();
    public DbSet<Reminder>        Reminders         => Set<Reminder>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<CitizenService>  CitizenServices   => Set<CitizenService>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Scans this assembly for all IEntityTypeConfiguration<T> classes
        // and applies them automatically.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
