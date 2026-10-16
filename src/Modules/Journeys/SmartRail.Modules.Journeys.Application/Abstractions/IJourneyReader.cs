using SmartRail.Application.Core.Abstractions;
using SmartRail.Modules.Journeys.Domain.Journeys;

namespace SmartRail.Modules.Journeys.Application.Abstractions;

// This module's audit log (its own audit_entries table, saved with the module's unit of work).
public interface IJourneysAuditLog : IAuditLog;

// Read side of the module: plain read models for queries.
public interface IJourneyReader
{
    Task<JourneyReadModel?> GetJourneyAsync(Guid journeyId, CancellationToken cancellationToken);

    // Departures from a station on the given service dates. A row is returned while its scheduled departure is at or after
    // notBeforeUtc (or at or after cancelledNotBeforeUtc when it is Cancelled), sorted by scheduled time, at most `count`.
    Task<IReadOnlyList<BoardRowReadModel>> GetBoardAsync(
        string stationCode,
        IReadOnlyCollection<DateOnly> serviceDates,
        DateTimeOffset notBeforeUtc,
        DateTimeOffset cancelledNotBeforeUtc,
        int count,
        CancellationToken cancellationToken);
}

public sealed record JourneyReadModel(
    Guid Id,
    DateOnly ServiceDate,
    string TrainNumber,
    string RouteCode,
    string OriginStationCode,
    string DestinationStationCode,
    JourneyLifecycle Lifecycle,
    JourneyConfidence Confidence,
    IReadOnlyList<JourneyStopReadModel> Stops);

public sealed record JourneyStopReadModel(
    int Sequence,
    string StationCode,
    JourneyStopKind Kind,
    JourneyStopState State,
    DateTimeOffset? ScheduledArrivalUtc,
    DateTimeOffset? ScheduledDepartureUtc,
    string? PlatformCode,
    PlatformState PlatformState);

public sealed record BoardRowReadModel(
    Guid JourneyId,
    string TrainNumber,
    string RouteCode,
    string DestinationStationCode,
    DateTimeOffset ScheduledDepartureUtc,
    string? PlatformCode,
    PlatformState PlatformState,
    JourneyLifecycle Lifecycle,
    JourneyConfidence Confidence);
