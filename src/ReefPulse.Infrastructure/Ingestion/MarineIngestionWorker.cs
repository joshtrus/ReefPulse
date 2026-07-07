using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReefPulse.Domain;

namespace ReefPulse.Infrastructure.Ingestion;

public sealed class MarineIngestionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IngestionOptions _options;
    private readonly ILogger<MarineIngestionWorker> _logger;

    public MarineIngestionWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<IngestionOptions> options,
        ILogger<MarineIngestionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Marine ingestion disabled; worker will not poll.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.IntervalMinutes));
        do
        {
            try
            {
                await PollOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Marine ingestion pass failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PollOnceAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReefDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<IOpenMeteoClient>();

        var sites = await db.ReefSites
            .Select(s => new { s.Id, s.Name, s.Latitude, s.Longitude })
            .ToListAsync(ct);

        var added = 0;
        foreach (var site in sites)
        {
            try
            {
                var snapshot = await client.GetCurrentAsync(site.Latitude, site.Longitude, ct);
                if (snapshot is null)
                {
                    _logger.LogWarning("No marine data returned for {Site}.", site.Name);
                    continue;
                }

                db.Readings.Add(new Reading
                {
                    ReefSiteId = site.Id,
                    Metric = MetricType.WaterTemperatureCelsius,
                    Value = snapshot.SeaSurfaceTemperatureCelsius,
                    ObservedAt = snapshot.ObservedAt,
                    Source = "open-meteo"
                });
                db.Readings.Add(new Reading
                {
                    ReefSiteId = site.Id,
                    Metric = MetricType.WaveHeightMeters,
                    Value = snapshot.WaveHeightMeters,
                    ObservedAt = snapshot.ObservedAt,
                    Source = "open-meteo"
                });
                added += 2;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to ingest marine data for {Site}.", site.Name);
            }
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Ingested {Count} readings across {Sites} sites.", added, sites.Count);
        }
    }
}
