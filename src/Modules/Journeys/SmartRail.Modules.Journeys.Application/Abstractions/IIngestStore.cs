using SmartRail.Modules.Journeys.Domain.Live;

namespace SmartRail.Modules.Journeys.Application.Abstractions;

// The write side of live ingest: the event history, raw telemetry, and the per-source sequence ledger.
// One sequence counter per source covers both stop events and positions (SPEC-005 decision D3).
public interface IIngestStore
{
    // Has this source already sent a message with this sequence, whatever became of it?
    Task<bool> SequenceSeenAsync(Guid sourceId, long sequence, CancellationToken cancellationToken);

    // The highest sequence of a message that was accepted (applied, superseded or held). Null when none.
    Task<long?> HighestAcceptedSequenceAsync(Guid sourceId, CancellationToken cancellationToken);

    // The event currently standing as the value of this fact (journey, stop, type), tracked so it can be superseded.
    Task<JourneyEvent?> FindWinningEventAsync(Guid journeyId, int stopSequence, StopEventType type, CancellationToken cancellationToken);

    // The journey this vehicle is assigned to at that moment, through the time-ranged assignment (REQ-LIVE-010).
    Task<Guid?> ResolveJourneyAsync(string vehicleCode, DateTimeOffset at, CancellationToken cancellationToken);

    // The vehicle's previous accepted reading on a journey, for the plausibility check.
    Task<TelemetryReading?> LastAcceptedReadingAsync(string vehicleCode, Guid journeyId, CancellationToken cancellationToken);

    void Add(JourneyEvent journeyEvent);

    void Add(TelemetryReading reading);
}
