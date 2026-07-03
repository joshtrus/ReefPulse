using Microsoft.EntityFrameworkCore;
using ReefPulse.Domain;

namespace ReefPulse.Infrastructure;

/// <summary>
/// The Entity Framework Core unit-of-work for ReefPulse. Owns the mapping between the
/// domain model and PostgreSQL. Per-entity mapping lives in the Configurations/ folder
/// (via <see cref="IEntityTypeConfiguration{TEntity}"/>) rather than bloating a single
/// OnModelCreating override.
/// </summary>
public class ReefDbContext : DbContext
{
    public ReefDbContext(DbContextOptions<ReefDbContext> options) : base(options)
    {
    }

    public DbSet<ReefSite> ReefSites => Set<ReefSite>();
    public DbSet<Reading> Readings => Set<Reading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReefDbContext).Assembly);
    }
}
