using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReefPulse.Infrastructure;

public sealed class ReefDbContextFactory : IDesignTimeDbContextFactory<ReefDbContext>
{
    public ReefDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReefDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=reefpulse;Username=reefpulse;Password=reefpulse")
            .Options;

        return new ReefDbContext(options);
    }
}
