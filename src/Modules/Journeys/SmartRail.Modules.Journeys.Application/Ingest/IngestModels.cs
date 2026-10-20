using System.Diagnostics.Metrics;
using SmartRail.Modules.Journeys.Domain.Live;

namespace SmartRail.Modules.Journeys.Application.Ingest;

// The authenticated sender of a batch. Rank 1 is the most trusted source (source-precedence.md).
public sealed record FeedSender(Guid Id, string Code, int Rank, EventSourceKind Kind = EventSourceKind.Feed);

// The answer for one message of a batch. A batch is never rejected as a whole because one item is bad.
public sealed record IngestItemResult(long Sequence, string Disposition, string? Reason = null, Guid? JourneyId = null);

public sealed record IngestResponse(IReadOnlyList<IngestItemResult> Results);

public static class IngestLimits
{
    public const int MaxBatch = 100;
}

// Accepted and rejected counts are observable (REQ-LIVE-005). The meter "SmartRail" is exported by ServiceDefaults.
internal static class IngestMetrics
{
    private static readonly Meter Meter = new("SmartRail");

    private static readonly Counter<long> AcceptedCounter =
        Meter.CreateCounter<long>("smartrail.ingest.accepted", description: "Feed messages applied, by kind");

    private static readonly Counter<long> RejectedCounter =
        Meter.CreateCounter<long>("smartrail.ingest.rejected", description: "Feed messages rejected, by reason");

    private static readonly Counter<long> DuplicateCounter =
        Meter.CreateCounter<long>("smartrail.ingest.duplicate", description: "Feed messages already received");

    private static readonly Counter<long> GhostCounter =
        Meter.CreateCounter<long>("smartrail.ingest.ghost", description: "Positions that match no journey (never shown publicly)");

    internal static void Accepted(string kind, string source) =>
        AcceptedCounter.Add(1, new KeyValuePair<string, object?>("kind", kind), new KeyValuePair<string, object?>("source", source));

    internal static void Rejected(string reason, string source) =>
        RejectedCounter.Add(1, new KeyValuePair<string, object?>("reason", reason), new KeyValuePair<string, object?>("source", source));

    internal static void Duplicate(string source) =>
        DuplicateCounter.Add(1, new KeyValuePair<string, object?>("source", source));

    internal static void Ghost(string source) =>
        GhostCounter.Add(1, new KeyValuePair<string, object?>("source", source));

    internal static string Name(EventDisposition disposition) => disposition.ToString();
}
