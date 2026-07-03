namespace ReefPulse.Domain;

/// <summary>
/// A monitored reef location — the aggregate that environmental <see cref="Reading"/>s
/// belong to. This is slow-changing reference data (a handful of sites), as opposed to
/// the high-volume time-series readings that stream in against it.
/// </summary>
public class ReefSite
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    /// <summary>Human-readable grouping, e.g. "Australia" or "Cozumel, Mexico".</summary>
    public string? Region { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public ICollection<Reading> Readings { get; } = new List<Reading>();
}
