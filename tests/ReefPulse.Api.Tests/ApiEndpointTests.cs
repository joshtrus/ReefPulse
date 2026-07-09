using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using ReefPulse.Domain;
using ReefPulse.Infrastructure;
using ReefPulse.Infrastructure.Ingestion;
using ReefPulse.Infrastructure.Messaging;
using Testcontainers.PostgreSql;
using Xunit;

namespace ReefPulse.Api.Tests;

public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ReefDbContext>>();
            services.RemoveAll<ReefDbContext>();
            services.AddDbContext<ReefDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));


            services.RemoveAll<IHostedService>();
        });
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}

public sealed class ApiEndpointTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public ApiEndpointTests(PostgresApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Liveness_probe_returns_200_and_healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Readiness_probe_is_healthy_when_database_is_reachable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Sites_endpoint_returns_seeded_reef_sites()
    {
        var client = _factory.CreateClient();

        var sites = await client.GetFromJsonAsync<List<SiteResponse>>("/sites");

        Assert.NotNull(sites);
        Assert.Equal(5, sites!.Count);
        Assert.Contains(sites, s => s.Name == "Negril");
    }

    [Fact]
    public async Task Ingestor_publishes_a_temperature_and_wave_event_per_site()
    {
        _ = _factory.CreateClient();   // boot the app so migrations run and reef sites are seeded

        var snapshot = new MarineSnapshot(
            ObservedAt: new DateTimeOffset(2026, 7, 6, 12, 0, 0, TimeSpan.Zero),
            SeaSurfaceTemperatureCelsius: 29.5,
            WaveHeightMeters: 1.2);
        var fakeClient = new FakeOpenMeteoClient(snapshot);
        var fakeProducer = new FakeReadingProducer();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReefDbContext>();

        var siteCount = await db.ReefSites.CountAsync();
        var published = await MarineIngestor.IngestAsync(db, fakeClient, fakeProducer, NullLogger.Instance);

        Assert.Equal(siteCount * 2, published);
        Assert.Equal(siteCount * 2, fakeProducer.Published.Count);
        Assert.All(
            fakeProducer.Published.Where(e => e.Metric == MetricType.WaterTemperatureCelsius),
            e => Assert.Equal(29.5, e.Value, 3));
        Assert.All(
            fakeProducer.Published.Where(e => e.Metric == MetricType.WaveHeightMeters),
            e => Assert.Equal(1.2, e.Value, 3));
    }

    [Fact]
    public async Task Persisting_the_same_event_twice_saves_only_one_reading()
    {
        _ = _factory.CreateClient();

        Guid siteId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ReefDbContext>();
            siteId = await db.ReefSites.Select(s => s.Id).FirstAsync();
        }

        var reading = new ReadingEvent(
            SiteId: siteId,
            Metric: MetricType.WaterTemperatureCelsius,
            Value: 29.5,
            ObservedAt: new DateTimeOffset(2026, 7, 8, 12, 0, 0, TimeSpan.Zero),
            Source: "idempotency-test");

        var first = await PersistInNewScope(reading);
        var second = await PersistInNewScope(reading);

        Assert.True(first);    // inserted
        Assert.False(second);  // duplicate skipped

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ReefDbContext>();
            var count = await db.Readings.CountAsync(r => r.Source == "idempotency-test");
            Assert.Equal(1, count);
        }
    }

    private async Task<bool> PersistInNewScope(ReadingEvent reading)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReefDbContext>();
        return await ReadingPersister.PersistAsync(db, reading);
    }

    private sealed record SiteResponse(Guid Id, string Name, string? Region, double Latitude, double Longitude);
}
