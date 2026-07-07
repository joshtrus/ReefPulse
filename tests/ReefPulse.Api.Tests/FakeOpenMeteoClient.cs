using ReefPulse.Infrastructure.Ingestion;

namespace ReefPulse.Api.Tests;

internal sealed class FakeOpenMeteoClient : IOpenMeteoClient
{
    private readonly MarineSnapshot? _snapshot;

    public FakeOpenMeteoClient(MarineSnapshot? snapshot) => _snapshot = snapshot;

    public Task<MarineSnapshot?> GetCurrentAsync(
        double latitude, double longitude, CancellationToken ct = default)
        => Task.FromResult(_snapshot);
}
