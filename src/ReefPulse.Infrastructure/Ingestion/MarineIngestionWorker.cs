using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ReefPulse.Infrastructure.Ingestion;

public sealed class MarineIngestionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IngestionOptions _options;
    private readonly ILogger<MarineIngestionWorker> _logger;

    public MarineIngestionWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<IngestionOptions> options,
        ILogger<MarineIngestionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Marine ingestion disabled; worker will not poll.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.IntervalMinutes));
        do
        {
            try
            {
                await PollOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Marine ingestion pass failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PollOnceAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReefDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<IOpenMeteoClient>();

        await MarineIngestor.IngestAsync(db, client, _logger, ct);
    }
}
