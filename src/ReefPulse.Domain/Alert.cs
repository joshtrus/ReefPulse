namespace ReefPulse.Domain;

public enum AlertStatus
{
    Active,
    Resolved
}

public class Alert
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Guid ReefSiteId { get; init; }
    public ReefSite? ReefSite { get; init; }

    public required MetricType Metric { get; init; }
    public required double Threshold { get; init; }
    public required double TriggeredValue { get; init; }

    public required DateTimeOffset TriggeredAt { get; init; }

    public DateTimeOffset? ResolvedAt { get; set; }
    public AlertStatus Status { get; set; } = AlertStatus.Active;
}
