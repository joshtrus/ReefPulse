using ReefPulse.Infrastructure.Messaging;

namespace ReefPulse.Api.Tests;

internal sealed class FakeReadingProducer : IReadingProducer
{
    public List<ReadingEvent> Published { get; } = new();

    public Task PublishAsync(ReadingEvent reading, CancellationToken ct = default)
    {
        Published.Add(reading);
        return Task.CompletedTask;
    }
}
