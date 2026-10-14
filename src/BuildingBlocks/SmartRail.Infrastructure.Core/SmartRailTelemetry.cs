using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SmartRail.Infrastructure.Core;

// Business telemetry. The meter and source names are registered in ServiceDefaults.
// Slices add their own instruments here (events ingested, journeys per confidence, board latency).
public static class SmartRailTelemetry
{
    public const string Name = "SmartRail";

    public static readonly ActivitySource ActivitySource = new(Name);

    private static readonly Meter Meter = new(Name);

    public static readonly Counter<long> OutboxProcessed =
        Meter.CreateCounter<long>("smartrail.outbox.processed", description: "Outbox messages dispatched successfully");

    public static readonly Counter<long> OutboxFailed =
        Meter.CreateCounter<long>("smartrail.outbox.failed", description: "Outbox dispatch attempts that failed");

    public static readonly Counter<long> OutboxDeadLettered =
        Meter.CreateCounter<long>("smartrail.outbox.dead_lettered", description: "Outbox messages that exhausted their attempts");
}
