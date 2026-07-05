using Microsoft.EntityFrameworkCore;
using ReefPulse.Domain;

namespace ReefPulse.Infrastructure;

/// <summary>
/// Idempotent reference-data seeding. Populates a few well-known reef sites so the API
/// returns something meaningful immediately after startup. Safe to call on every boot:
/// it no-ops once sites exist.
/// </summary>
public static class SeedData
{
    public static async Task EnsureSeedAsync(ReefDbContext db, CancellationToken ct = default)
    {
        if (await db.ReefSites.AnyAsync(ct))
        {
            return;
        }

        db.ReefSites.AddRange(
            new ReefSite { Name = "Montego Bay", Region = "St. James", Latitude = 18.4700, Longitude = -77.9500 },
            new ReefSite { Name = "Negril", Region = "Westmoreland", Latitude = 18.2686, Longitude = -78.3466 },
            new ReefSite { Name = "Discovery Bay", Region = "St. Ann", Latitude = 18.4667, Longitude = -77.4100 },
            new ReefSite { Name = "Oracabessa Bay Fish Sanctuary", Region = "St. Mary", Latitude = 18.4062, Longitude = -76.9522 },
            new ReefSite { Name = "Port Royal Cays", Region = "Kingston", Latitude = 17.9366, Longitude = -76.8414 });

        await db.SaveChangesAsync(ct);
    }
}
