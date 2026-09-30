using Microsoft.EntityFrameworkCore;
using MojRok.Domain.Entities;

namespace MojRok.Application.Abstractions;

/// <summary>
/// Abstraction over AppDbContext used by Application service classes.
/// Keeps Application layer independent of Npgsql and the concrete context.
/// Infrastructure implements this interface; Application never references it directly.
/// </summary>
public interface IAppDbContext
{
    DbSet<AppUser>         Users             { get; }
    DbSet<Municipality>    Municipalities    { get; }
    DbSet<Category>        Categories        { get; }
    DbSet<Deadline>        Deadlines         { get; }
    DbSet<Reminder>        Reminders         { get; }
    DbSet<ServiceCategory> ServiceCategories { get; }
    DbSet<CitizenService>  CitizenServices   { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
