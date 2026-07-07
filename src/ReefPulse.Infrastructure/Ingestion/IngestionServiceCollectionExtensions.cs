using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ReefPulse.Infrastructure.Ingestion;

public static class IngestionServiceCollectionExtensions
{
    public static IServiceCollection AddReefIngestion(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IngestionOptions>(
            configuration.GetSection(IngestionOptions.SectionName));

        services.AddHttpClient<IOpenMeteoClient, OpenMeteoClient>(client =>
        {
            client.BaseAddress = new Uri("https://marine-api.open-meteo.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHostedService<MarineIngestionWorker>();

        return services;
    }
}
