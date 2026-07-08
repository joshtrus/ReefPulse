using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace ReefPulse.Infrastructure.Messaging;

public interface IReadingProducer
{
    Task PublishAsync(ReadingEvent reading, CancellationToken ct = default);
}

public sealed class KafkaReadingProducer : IReadingProducer, IDisposable
{
    private readonly IProducer<string, ReadingEvent> _producer;
    private readonly string _topic;

    public KafkaReadingProducer(IOptions<KafkaOptions> options)
    {
        _topic = options.Value.ReadingsTopic;

        var config = new ProducerConfig { BootstrapServers = options.Value.BootstrapServers };
        _producer = new ProducerBuilder<string, ReadingEvent>(config)
            .SetValueSerializer(new ReadingEventJsonSerializer())
            .Build();
    }

    public async Task PublishAsync(ReadingEvent reading, CancellationToken ct = default)
    {
        var message = new Message<string, ReadingEvent>
        {
            Key = reading.SiteId.ToString(),
            Value = reading
        };

        await _producer.ProduceAsync(_topic, message, ct);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
