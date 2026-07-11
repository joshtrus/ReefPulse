using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ReefPulse.Infrastructure.Detection;

public static class DetectionServiceCollectionExtensions
{
    public static IServiceCollection AddReefDetection(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BleachingOptions>(
            configuration.GetSection(BleachingOptions.SectionName));

        return services;
    }
}
