using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ReefPulse.Infrastructure.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddReefMessaging(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(
            configuration.GetSection(KafkaOptions.SectionName));

        services.AddHostedService<KafkaTopicInitializer>();
        services.AddSingleton<IReadingProducer, KafkaReadingProducer>();

        return services;
    }
}
