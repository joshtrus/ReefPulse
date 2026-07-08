using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReefPulse.Domain;
using ReefPulse.Infrastructure.Messaging;

namespace ReefPulse.Infrastructure.Ingestion;

public static class MarineIngestor
{
    public static async Task<int> IngestAsync(
        ReefDbContext db, IOpenMeteoClient client, IReadingProducer producer, ILogger logger,
        CancellationToken ct = default)
    {
        var sites = await db.ReefSites
            .Select(s => new { s.Id, s.Name, s.Latitude, s.Longitude })
            .ToListAsync(ct);

        var published = 0;
        foreach (var site in sites)
        {
            try
            {
                var snapshot = await client.GetCurrentAsync(site.Latitude, site.Longitude, ct);
                if (snapshot is null)
                {
                    logger.LogWarning("No marine data returned for {Site}.", site.Name);
                    continue;
                }

                await producer.PublishAsync(new ReadingEvent(
                    site.Id, MetricType.WaterTemperatureCelsius,
                    snapshot.SeaSurfaceTemperatureCelsius, snapshot.ObservedAt, "open-meteo"), ct);
                await producer.PublishAsync(new ReadingEvent(
                    site.Id, MetricType.WaveHeightMeters,
                    snapshot.WaveHeightMeters, snapshot.ObservedAt, "open-meteo"), ct);
                published += 2;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to ingest marine data for {Site}.", site.Name);
            }
        }

        if (published > 0)
        {
            logger.LogInformation("Published {Count} reading events across {Sites} sites.", published, sites.Count);
        }

        return published;
    }
}
