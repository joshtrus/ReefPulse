using ReefPulse.Domain;

namespace ReefPulse.Api.Contracts;

public record CreateReadingRequest(
    MetricType Metric,
    double Value,
    DateTimeOffset ObservedAt,
    string Source);
