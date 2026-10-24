using SmartRail.Application.Core.Abstractions;
using SmartRail.Modules.Journeys.Domain.Journeys;

namespace SmartRail.Modules.Journeys.Application.Abstractions;

// This module's audit log (its own audit_entries table, saved with the module's unit of work).
public interface IJourneysAuditLog : IAuditLog;

// Read side of the module: plain read models for queries.
public interface IJourneyReader
{
    Task<JourneyReadModel?> GetJourneyAsync(Guid journeyId, CancellationToken cancellationToken);

    // Departures from a station on the given service dates, sorted by the departure time clients will be told
    // (estimated when there is one, otherwise scheduled), at most `count`.
    //
    // A row is returned while:
    //   - its scheduled departure is at or after notBeforeUtc, or
    //   - the train actually left at or after notBeforeUtc, or
    //   - the train is Running, has not yet left this station, and was scheduled at or after runningNotBeforeUtc
    //     (a late train stays on the board until it leaves),
    // and a Cancelled journey while its scheduled departure is at or after cancelledNotBeforeUtc.
    Task<IReadOnlyList<BoardRowReadModel>> GetBoardAsync(
        string stationCode,
        IReadOnlyCollection<DateOnly> serviceDates,
        DateTimeOffset notBeforeUtc,
        DateTimeOffset cancelledNotBeforeUtc,
        DateTimeOffset runningNotBeforeUtc,
        int count,
        CancellationToken cancellationToken);

    // The arrivals board: trains that call at the station and have an arrival time there, from the same dates. A row stays for
    // a minute after its actual arrival (a train that finished its journey here included), a Cancelled journey while its scheduled
    // arrival is at or after cancelledNotBeforeUtc, and a Running train that has not arrived yet while its scheduled arrival is at
    // or after runningNotBeforeUtc.
    Task<IReadOnlyList<ArrivalRowReadModel>> GetArrivalsAsync(
        string stationCode,
        IReadOnlyCollection<DateOnly> serviceDates,
        DateTimeOffset arrivedNotBeforeUtc,
        DateTimeOffset cancelledNotBeforeUtc,
        DateTimeOffset runningNotBeforeUtc,
        int count,
        CancellationToken cancellationToken);

    // Journeys (not Completed, not Cancelled) on the given service dates that visit either station and have a stop
    // at or after notBeforeUtc. The search joins them in memory.
    Task<IReadOnlyList<JourneyReadModel>> GetSearchCandidatesAsync(
        string fromStationCode,
        string toStationCode,
        IReadOnlyCollection<DateOnly> serviceDates,
        DateTimeOffset notBeforeUtc,
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
    int? DelayMinutes,
    PunctualityBand? Punctuality,
    DateTimeOffset? LastAcceptedUpdateAt,
    IReadOnlyList<JourneyStopReadModel> Stops);

public sealed record JourneyStopReadModel(
    int Sequence,
    string StationCode,
    JourneyStopKind Kind,
    JourneyStopState State,
    DateTimeOffset? ScheduledArrivalUtc,
    DateTimeOffset? ScheduledDepartureUtc,
    DateTimeOffset? EstimatedArrivalUtc,
    DateTimeOffset? EstimatedDepartureUtc,
    DateTimeOffset? ActualArrivalUtc,
    DateTimeOffset? ActualDepartureUtc,
    string? PlatformCode,
    PlatformState PlatformState);

public sealed record BoardRowReadModel(
    Guid JourneyId,
    string TrainNumber,
    string RouteCode,
    string DestinationStationCode,
    DateTimeOffset ScheduledDepartureUtc,
    DateTimeOffset? EstimatedDepartureUtc,
    DateTimeOffset? ActualDepartureUtc,
    string? PlatformCode,
    PlatformState PlatformState,
    JourneyLifecycle Lifecycle,
    JourneyConfidence Confidence,
    int? DelayMinutes,
    PunctualityBand? Punctuality,
    DateTimeOffset? LastAcceptedUpdateAt,
    JourneyStopState StopState);

public sealed record ArrivalRowReadModel(
    Guid JourneyId,
    string TrainNumber,
    string RouteCode,
    string OriginStationCode,
    DateTimeOffset ScheduledArrivalUtc,
    DateTimeOffset? EstimatedArrivalUtc,
    DateTimeOffset? ActualArrivalUtc,
    string? PlatformCode,
    PlatformState PlatformState,
    JourneyLifecycle Lifecycle,
    JourneyConfidence Confidence,
    int? DelayMinutes,
    PunctualityBand? Punctuality,
    DateTimeOffset? LastAcceptedUpdateAt);
