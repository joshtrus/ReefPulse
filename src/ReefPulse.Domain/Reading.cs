namespace ReefPulse.Domain;

public enum MetricType
{
    WaterTemperatureCelsius,
    TideHeightMeters,
    WaveHeightMeters,
    SalinityPsu,
    DissolvedOxygenMgPerL
}


public class Reading
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Guid ReefSiteId { get; init; }

    public ReefSite? ReefSite { get; init; }

    public required MetricType Metric { get; init; }
    public required double Value { get; init; }
    public required DateTimeOffset ObservedAt { get; init; }
    
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;
    
    public required string Source { get; init; }
}
