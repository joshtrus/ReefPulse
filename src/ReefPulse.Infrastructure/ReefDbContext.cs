using Microsoft.EntityFrameworkCore;
using ReefPulse.Domain;

namespace ReefPulse.Infrastructure;

public class ReefDbContext : DbContext
{
    public ReefDbContext(DbContextOptions<ReefDbContext> options) : base(options)
    {
    }

    public DbSet<ReefSite> ReefSites => Set<ReefSite>();
    public DbSet<Reading> Readings => Set<Reading>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReefDbContext).Assembly);
    }
}
