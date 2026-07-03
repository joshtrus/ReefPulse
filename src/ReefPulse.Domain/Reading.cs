namespace ReefPulse.Domain;

/// <summary>The kind of environmental measurement a <see cref="Reading"/> carries.</summary>
public enum MetricType
{
    WaterTemperatureCelsius,
    TideHeightMeters,
    WaveHeightMeters,
    SalinityPsu,
    DissolvedOxygenMgPerL
}

/// <summary>
/// A single environmental measurement taken at a <see cref="ReefSite"/>.
/// This is the unit that will later flow through the ingestion pipeline (Kafka) and be
/// scanned for anomalies, so it deliberately separates event time from processing time.
/// </summary>
public class Reading
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Guid ReefSiteId { get; init; }

    /// <summary>Navigation to the owning site. Null unless explicitly loaded.</summary>
    public ReefSite? ReefSite { get; init; }

    public required MetricType Metric { get; init; }
    public required double Value { get; init; }

    /// <summary>When the phenomenon was actually measured at the source (event time).</summary>
    public required DateTimeOffset ObservedAt { get; init; }

    /// <summary>
    /// When ReefPulse ingested the reading (processing time). The gap between this and
    /// <see cref="ObservedAt"/> is end-to-end pipeline lag — a metric we'll want to watch.
    /// </summary>
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Provenance of the reading, e.g. "NOAA", "CoralReefWatch", "synthetic".</summary>
    public required string Source { get; init; }
}
