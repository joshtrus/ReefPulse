using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ReefPulse.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddReefPersistence(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ReefDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
