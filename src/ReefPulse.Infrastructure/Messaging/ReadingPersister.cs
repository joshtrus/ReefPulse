using ReefPulse.Domain;

namespace ReefPulse.Infrastructure.Messaging;

public static class ReadingPersister
{
    public static async Task PersistAsync(ReefDbContext db, ReadingEvent reading, CancellationToken ct = default)
    {
        db.Readings.Add(new Reading
        {
            ReefSiteId = reading.SiteId,
            Metric = reading.Metric,
            Value = reading.Value,
            ObservedAt = reading.ObservedAt,
            Source = reading.Source
        });

        await db.SaveChangesAsync(ct);
    }
}
