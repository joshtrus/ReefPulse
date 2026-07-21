using System.Diagnostics.Metrics;

namespace ReefPulse.Infrastructure.Observability;

public sealed class ReefMetrics : IDisposable
{
    public const string MeterName = "ReefPulse";

    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _readingsPublished;
    private readonly Counter<long> _readingsPersisted;
    private readonly Counter<long> _alertsOpened;
    private readonly Counter<long> _alertsResolved;
    private readonly Histogram<double> _ingestLagSeconds;

    public ReefMetrics()
    {
        _readingsPublished = _meter.CreateCounter<long>("reef.readings.published");
        _readingsPersisted = _meter.CreateCounter<long>("reef.readings.persisted");
        _alertsOpened = _meter.CreateCounter<long>("reef.alerts.opened");
        _alertsResolved = _meter.CreateCounter<long>("reef.alerts.resolved");
        _ingestLagSeconds = _meter.CreateHistogram<double>("reef.ingest.lag.seconds");
    }

    public void ReadingPublished() => _readingsPublished.Add(1);
    public void ReadingPersisted() => _readingsPersisted.Add(1);
    public void AlertOpened() => _alertsOpened.Add(1);
    public void AlertResolved() => _alertsResolved.Add(1);
    public void RecordIngestLagSeconds(double seconds) => _ingestLagSeconds.Record(seconds);

    public void Dispose() => _meter.Dispose();
}
