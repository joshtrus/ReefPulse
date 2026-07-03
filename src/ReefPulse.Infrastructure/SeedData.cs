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
            new ReefSite { Name = "Great Barrier Reef — Cairns", Region = "Australia", Latitude = -16.92, Longitude = 145.77 },
            new ReefSite { Name = "Palancar Reef", Region = "Cozumel, Mexico", Latitude = 20.35, Longitude = -87.02 },
            new ReefSite { Name = "Tubbataha Reefs", Region = "Philippines", Latitude = 8.85, Longitude = 119.92 });

        await db.SaveChangesAsync(ct);
    }
}
