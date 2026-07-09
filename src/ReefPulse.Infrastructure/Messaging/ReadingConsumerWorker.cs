using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ReefPulse.Infrastructure.Messaging;

public sealed class ReadingConsumerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<ReadingConsumerWorker> _logger;

    public ReadingConsumerWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<ReadingConsumerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    // Run the blocking consume loop on a background thread so it doesn't hold up host startup.
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private async Task ConsumeLoop(CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, ReadingEvent>(config)
            .SetValueDeserializer(new ReadingEventJsonSerializer())
            .Build();

        consumer.Subscribe(_options.ReadingsTopic);
        _logger.LogInformation(
            "Consuming '{Topic}' as group '{Group}'.", _options.ReadingsTopic, _options.ConsumerGroup);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(ct);
                if (result?.Message?.Value is null)
                    continue;

                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ReefDbContext>();
                await ReadingPersister.PersistAsync(db, result.Message.Value, ct);

                consumer.Commit(result);   // at-least-once: commit only after a successful save
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Don't commit: the message will be re-delivered on restart/rebalance.
                _logger.LogError(ex, "Failed to consume/persist a reading event.");
            }
        }

        consumer.Close();
    }
}
