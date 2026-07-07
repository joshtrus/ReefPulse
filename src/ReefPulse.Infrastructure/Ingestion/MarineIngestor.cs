using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReefPulse.Domain;

namespace ReefPulse.Infrastructure.Ingestion;

public static class MarineIngestor
{
    public static async Task<int> IngestAsync(
        ReefDbContext db, IOpenMeteoClient client, ILogger logger, CancellationToken ct = default)
    {
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
                    logger.LogWarning("No marine data returned for {Site}.", site.Name);
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
                logger.LogError(ex, "Failed to ingest marine data for {Site}.", site.Name);
            }
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Ingested {Count} readings across {Sites} sites.", added, sites.Count);
        }

        return added;
    }
}
