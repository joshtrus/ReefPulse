using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReefPulse.Domain;
using ReefPulse.Infrastructure.Messaging;
using ReefPulse.Infrastructure.Observability;

namespace ReefPulse.Infrastructure.Detection;

public sealed class BleachingDetectionWorker : BackgroundService
{
    private const string DetectorGroup = "reefpulse-detectors";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ReefMetrics _metrics;
    private readonly KafkaOptions _kafka;
    private readonly BleachingOptions _bleaching;
    private readonly ILogger<BleachingDetectionWorker> _logger;

    public BleachingDetectionWorker(
        IServiceScopeFactory scopeFactory,
        ReefMetrics metrics,
        IOptions<KafkaOptions> kafka,
        IOptions<BleachingOptions> bleaching,
        ILogger<BleachingDetectionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _metrics = metrics;
        _kafka = kafka.Value;
        _bleaching = bleaching.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private async Task ConsumeLoop(CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafka.BootstrapServers,
            GroupId = DetectorGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, ReadingEvent>(config)
            .SetValueDeserializer(new ReadingEventJsonSerializer())
            .Build();

        consumer.Subscribe(_kafka.ReadingsTopic);
        _logger.LogInformation(
            "Bleaching detector consuming '{Topic}' as group '{Group}'.", _kafka.ReadingsTopic, DetectorGroup);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(ct);
                if (result?.Message?.Value is null)
                    continue;

                await ProcessAsync(result.Message.Value, ct);
                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bleaching detection failed for a reading.");
            }
        }

        consumer.Close();
    }

    private async Task ProcessAsync(ReadingEvent reading, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReefDbContext>();

        var activeAlert = await db.Alerts
            .Where(a => a.ReefSiteId == reading.SiteId && a.Status == AlertStatus.Active)
            .FirstOrDefaultAsync(ct);

        switch (BleachingDetector.Evaluate(reading, activeAlert, _bleaching.TemperatureThresholdCelsius))
        {
            case AlertAction.Open:
                db.Alerts.Add(new Alert
                {
                    ReefSiteId = reading.SiteId,
                    Metric = reading.Metric,
                    Threshold = _bleaching.TemperatureThresholdCelsius,
                    TriggeredValue = reading.Value,
                    TriggeredAt = reading.ObservedAt
                });
                await db.SaveChangesAsync(ct);
                _metrics.AlertOpened();
                _logger.LogWarning(
                    "Bleaching alert OPENED for site {Site} at {Value}°C.", reading.SiteId, reading.Value);
                break;

            case AlertAction.Resolve:
                activeAlert!.Status = AlertStatus.Resolved;
                activeAlert.ResolvedAt = reading.ObservedAt;
                await db.SaveChangesAsync(ct);
                _metrics.AlertResolved();
                _logger.LogInformation(
                    "Bleaching alert RESOLVED for site {Site} at {Value}°C.", reading.SiteId, reading.Value);
                break;

            case AlertAction.None:
            default:
                break;
        }
    }
}
