using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReefPulse.Domain;
using ReefPulse.Infrastructure;
using ReefPulse.Infrastructure.Ingestion;
using ReefPulse.Infrastructure.Messaging;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using Xunit;

namespace ReefPulse.Api.Tests;

public sealed class KafkaPipelineFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();
    private readonly KafkaContainer _kafka = new KafkaBuilder("confluentinc/cp-kafka:7.6.0").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ReefDbContext>>();
            services.RemoveAll<ReefDbContext>();
            services.AddDbContext<ReefDbContext>(o => o.UseNpgsql(_postgres.GetConnectionString()));

            // Point Kafka at the test broker and don't hit Open-Meteo; keep hosted services
            // (producer, consumer, topic-init) running so the real pipeline is exercised.
            var bootstrap = _kafka.GetBootstrapAddress().Replace("PLAINTEXT://", "");
            services.PostConfigure<KafkaOptions>(o => o.BootstrapServers = bootstrap);
            services.PostConfigure<IngestionOptions>(o => o.Enabled = false);
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _kafka.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _kafka.DisposeAsync();
        await base.DisposeAsync();
    }
}

public sealed class PipelineEndToEndTests : IClassFixture<KafkaPipelineFactory>
{
    private readonly KafkaPipelineFactory _factory;

    public PipelineEndToEndTests(KafkaPipelineFactory factory) => _factory = factory;

    [Fact]
    public async Task Published_event_is_persisted_by_the_consumer()
    {
        _ = _factory.CreateClient();   // boot app: topic-init + consumer start + migrate/seed

        Guid siteId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ReefDbContext>();
            siteId = await db.ReefSites.Select(s => s.Id).FirstAsync();
        }

        var producer = _factory.Services.GetRequiredService<IReadingProducer>();
        var reading = new ReadingEvent(
            SiteId: siteId,
            Metric: MetricType.WaterTemperatureCelsius,
            Value: 27.3,
            ObservedAt: new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero),
            Source: "e2e-test");

        await producer.PublishAsync(reading);

        var persisted = await WaitForReadingAsync("e2e-test", TimeSpan.FromSeconds(30));
        Assert.True(persisted, "consumer did not persist the published reading within the timeout");
    }

    private async Task<bool> WaitForReadingAsync(string source, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ReefDbContext>();
            if (await db.Readings.AnyAsync(r => r.Source == source))
                return true;
            await Task.Delay(500);
        }

        return false;
    }
}
