using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ReefPulse.Infrastructure;

/// <summary>
/// Composition-root wiring for the persistence layer. Keeping this here means the API's
/// Program.cs asks for "ReefPulse persistence" without knowing it's EF Core + Npgsql —
/// the provider choice stays an infrastructure detail.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddReefPersistence(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ReefDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
