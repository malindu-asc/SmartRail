namespace SmartRail.Simulator;

// Replays a seeded service through the real ingestion endpoint (see docs/02-architecture/simulator-design.md).
// Scenarios (delay, cancel, feed stop) are added as the Journeys ingestion endpoint is built.
public sealed class SimulatorWorker(IngestionClient ingestion, ILogger<SimulatorWorker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Simulator started against {Api}", ingestion.BaseAddress);
        return Task.CompletedTask;
    }
}

public sealed class IngestionClient(HttpClient http)
{
    public Uri? BaseAddress => http.BaseAddress;
}
