using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReefPulse.Domain;
using ReefPulse.Infrastructure.Observability;

namespace ReefPulse.Infrastructure.Messaging;

public static class ReadingPersister
{
    public static async Task<bool> PersistAsync(
        ReefDbContext db, ReadingEvent reading, ReefMetrics? metrics = null, CancellationToken ct = default)
    {
        db.Readings.Add(new Reading
        {
            ReefSiteId = reading.SiteId,
            Metric = reading.Metric,
            Value = reading.Value,
            ObservedAt = reading.ObservedAt,
            Source = reading.Source
        });

        try
        {
            await db.SaveChangesAsync(ct);
            metrics?.ReadingPersisted();
            metrics?.RecordIngestLagSeconds((DateTimeOffset.UtcNow - reading.ObservedAt).TotalSeconds);
            return true;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Duplicate delivery (at-least-once): the reading already exists, so this is a no-op.
            return false;
        }
    }
}
