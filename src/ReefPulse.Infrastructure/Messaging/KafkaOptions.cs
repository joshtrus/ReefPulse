namespace ReefPulse.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ReadingsTopic { get; set; } = "reef.readings";
    public string ConsumerGroup { get; set; } = "reefpulse-consumers";
}
