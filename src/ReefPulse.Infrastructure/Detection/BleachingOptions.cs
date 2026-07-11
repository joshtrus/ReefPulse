namespace ReefPulse.Infrastructure.Detection;

public sealed class BleachingOptions
{
    public const string SectionName = "Bleaching";

    public double TemperatureThresholdCelsius { get; set; } = 30.5;
}
