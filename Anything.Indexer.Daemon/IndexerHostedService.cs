using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Anything.Indexer.Daemon;

public sealed class IndexerHostedService : BackgroundService
{
    private readonly FileIndexerDaemon _daemon;
    private readonly ILogger<IndexerHostedService> _logger;

    public IndexerHostedService(FileIndexerDaemon daemon, ILogger<IndexerHostedService> logger)
    {
        _daemon = daemon;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Anything Indexer Daemon starting");
        await _daemon.StartAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Anything Indexer Daemon stopping");
        await _daemon.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
