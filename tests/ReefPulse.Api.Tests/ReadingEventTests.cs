using ReefPulse.Domain;
using ReefPulse.Infrastructure.Messaging;
using Xunit;

namespace ReefPulse.Api.Tests;

public sealed class ReadingEventTests
{
    [Fact]
    public void ReadingEvent_round_trips_through_json()
    {
        var serde = new ReadingEventJsonSerializer();
        var original = new ReadingEvent(
            SiteId: Guid.NewGuid(),
            Metric: MetricType.WaterTemperatureCelsius,
            Value: 29.5,
            ObservedAt: new DateTimeOffset(2026, 7, 7, 1, 30, 0, TimeSpan.Zero),
            Source: "open-meteo");

        var bytes = serde.Serialize(original, default);
        var result = serde.Deserialize(bytes, isNull: false, default);

        Assert.Equal(original, result);
    }
}
