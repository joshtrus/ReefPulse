using ReefPulse.Domain;

namespace ReefPulse.Infrastructure.Messaging;

public sealed record ReadingEvent(
    Guid SiteId,
    MetricType Metric,
    double Value,
    DateTimeOffset ObservedAt,
    string Source);
