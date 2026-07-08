using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;

namespace ReefPulse.Infrastructure.Messaging;

public sealed class ReadingEventJsonSerializer : ISerializer<ReadingEvent>, IDeserializer<ReadingEvent>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public byte[] Serialize(ReadingEvent data, SerializationContext context)
        => JsonSerializer.SerializeToUtf8Bytes(data, Options);

    public ReadingEvent Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        => JsonSerializer.Deserialize<ReadingEvent>(data, Options)
           ?? throw new InvalidOperationException("Failed to deserialize ReadingEvent.");
}
