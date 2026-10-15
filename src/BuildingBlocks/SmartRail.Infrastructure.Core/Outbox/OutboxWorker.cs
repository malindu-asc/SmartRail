using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartRail.Infrastructure.Core.Outbox;

// Polls every module's outbox. Single API instance assumed; scaling out needs FOR UPDATE SKIP LOCKED.
public sealed class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Outbox worker disabled by configuration");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(options.Value.IntervalMilliseconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            foreach (var processor in scope.ServiceProvider.GetServices<IOutboxMessageProcessor>())
            {
                try
                {
                    await processor.ProcessAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Outbox processor {Processor} failed", processor.GetType().Name);
                }
            }
        }
    }
}
