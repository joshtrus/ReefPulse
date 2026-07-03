using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReefPulse.Infrastructure;

/// <summary>
/// Used only by the EF Core CLI (`dotnet ef`) at design time to construct a context when
/// generating or scaffolding migrations. It is never used at runtime — the app builds its
/// own context from configuration via <see cref="InfrastructureServiceCollectionExtensions"/>.
/// The connection string here only needs to be structurally valid; migration generation
/// does not connect to a live database.
/// </summary>
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
