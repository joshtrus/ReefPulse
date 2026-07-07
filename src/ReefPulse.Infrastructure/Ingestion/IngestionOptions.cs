namespace ReefPulse.Infrastructure.Ingestion;

public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 10;
}
