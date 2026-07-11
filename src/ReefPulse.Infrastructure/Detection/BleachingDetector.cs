using ReefPulse.Domain;
using ReefPulse.Infrastructure.Messaging;

namespace ReefPulse.Infrastructure.Detection;

public enum AlertAction
{
    None,
    Open,
    Resolve
}

public static class BleachingDetector
{
    public static AlertAction Evaluate(ReadingEvent reading, Alert? activeAlert, double thresholdCelsius)
    {
        if (reading.Metric != MetricType.WaterTemperatureCelsius)
            return AlertAction.None;

        var isHot = reading.Value >= thresholdCelsius;

        if (isHot && activeAlert is null)
            return AlertAction.Open;

        if (!isHot && activeAlert is not null)
            return AlertAction.Resolve;

        return AlertAction.None;
    }
}
