using ReefPulse.Domain;
using ReefPulse.Infrastructure.Detection;
using ReefPulse.Infrastructure.Messaging;
using Xunit;

namespace ReefPulse.Api.Tests;

public sealed class BleachingDetectorTests
{
    private const double Threshold = 30.5;

    [Fact]
    public void Hot_reading_with_no_active_alert_opens_one()
        => Assert.Equal(AlertAction.Open,
            BleachingDetector.Evaluate(TempReading(31.0), activeAlert: null, Threshold));

    [Fact]
    public void Cool_reading_with_an_active_alert_resolves_it()
        => Assert.Equal(AlertAction.Resolve,
            BleachingDetector.Evaluate(TempReading(29.0), ActiveAlert(), Threshold));

    [Fact]
    public void Hot_reading_when_already_alerting_does_nothing()
        => Assert.Equal(AlertAction.None,
            BleachingDetector.Evaluate(TempReading(31.0), ActiveAlert(), Threshold));

    [Fact]
    public void Cool_reading_with_no_active_alert_does_nothing()
        => Assert.Equal(AlertAction.None,
            BleachingDetector.Evaluate(TempReading(29.0), activeAlert: null, Threshold));

    [Fact]
    public void Non_temperature_reading_is_ignored()
        => Assert.Equal(AlertAction.None, BleachingDetector.Evaluate(
            new ReadingEvent(Guid.NewGuid(), MetricType.WaveHeightMeters, 99.0, DateTimeOffset.UnixEpoch, "test"),
            activeAlert: null, Threshold));

    private static ReadingEvent TempReading(double value)
        => new(Guid.NewGuid(), MetricType.WaterTemperatureCelsius, value, DateTimeOffset.UnixEpoch, "test");

    private static Alert ActiveAlert()
        => new()
        {
            ReefSiteId = Guid.NewGuid(),
            Metric = MetricType.WaterTemperatureCelsius,
            Threshold = Threshold,
            TriggeredValue = 31.0,
            TriggeredAt = DateTimeOffset.UnixEpoch
        };
}
