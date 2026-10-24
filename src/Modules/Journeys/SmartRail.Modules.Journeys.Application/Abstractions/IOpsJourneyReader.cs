using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;

namespace SmartRail.Modules.Journeys.Application.Abstractions;

// Read side for the controller and scheduler consoles: everything about a journey in one flat record, not only what passengers see.
public interface IOpsJourneyReader
{
    // The journeys of one service date (sorted by scheduled departure), optionally narrowed.
    Task<IReadOnlyList<OpsJourneyData>> ListAsync(OpsJourneyFilter filter, CancellationToken cancellationToken);

    Task<OpsJourneyData?> GetAsync(Guid journeyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OpsEventData>> GetEventsAsync(Guid journeyId, int take, CancellationToken cancellationToken);

    // Other journeys that call at a platform of a station during a window (to warn before two trains are put on one platform).
    Task<IReadOnlyList<OpsPlatformUse>> GetPlatformUseAsync(
        string stationCode, string platformCode, DateTimeOffset from, DateTimeOffset to, Guid excludeJourneyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OpsVehicleData>> ListVehiclesAsync(CancellationToken cancellationToken);

    // Out-of-service periods that touch a window (or are still open when openOnly).
    Task<IReadOnlyList<OpsUnavailability>> ListUnavailabilityAsync(Guid? vehicleId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<IReadOnlyList<OpsDutyData>> ListDutiesAsync(DateOnly serviceDate, CancellationToken cancellationToken);

    // The windows a vehicle or a duty is already used for (not counting one journey, which is being changed).
    Task<IReadOnlyList<OpsSpan>> ListVehicleSpansAsync(Guid vehicleId, DateTimeOffset from, DateTimeOffset to, Guid? exceptJourneyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OpsSpan>> ListDutySpansAsync(Guid dutyId, Guid? exceptJourneyId, CancellationToken cancellationToken);

    // Journeys that finished, were cancelled or ended early between two service dates, with what the punctuality report needs.
    Task<IReadOnlyList<OpsPunctualityRow>> ListPunctualityAsync(DateOnly from, DateOnly to, string? routeCode, CancellationToken cancellationToken);

    // Corrections made by a controller or a better source, in a window (the "corrected records" figure of the report).
    Task<int> CountCorrectedEventsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
}

public sealed record OpsJourneyFilter(DateOnly ServiceDate, string? RouteCode, string? StationCode, string? Text);

public sealed record OpsStopData(
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
    string? PlatformOriginalCode,
    PlatformState PlatformState,
    EstimateSource EstimateSource);

public sealed record OpsJourneyData(
    Guid Id,
    DateOnly ServiceDate,
    string TrainNumber,
    string RouteCode,
    string OriginStationCode,
    string DestinationStationCode,
    DateTimeOffset ScheduledDepartureUtc,
    DateTimeOffset ScheduledArrivalUtc,
    JourneyLifecycle Lifecycle,
    JourneyConfidence Confidence,
    int? DelayMinutes,
    PunctualityBand? Punctuality,
    DateTimeOffset? LastAcceptedUpdateAt,
    int? CurrentStopSequence,
    int Version,
    string? CancelReasonCode,
    string? EndReasonCode,
    string? ControllerNote,
    DateTimeOffset? EndedAt,
    int? EndedAtStopSequence,
    Guid? VehicleId,
    string? VehicleCode,
    Guid? DutyId,
    string? DriverSubject,
    IReadOnlyList<OpsStopData> Stops);

public sealed record OpsEventData(
    long Id,
    int StopSequence,
    StopEventType Type,
    EventSourceKind SourceKind,
    DateTimeOffset OccurredAtSource,
    DateTimeOffset ReceivedAt,
    EventDisposition Disposition,
    string? DispositionReason);

public sealed record OpsPlatformUse(Guid JourneyId, string TrainNumber, string DestinationStationCode, DateTimeOffset From, DateTimeOffset To);

public sealed record OpsVehicleData(Guid Id, string Code, string Name, string Kind, bool IsActive);

public sealed record OpsUnavailability(Guid Id, Guid VehicleId, DateTimeOffset From, DateTimeOffset? To, string Reason, string CreatedBy, DateTimeOffset CreatedAt);

public sealed record OpsDutyData(Guid Id, string DriverSubject, DateOnly ServiceDate, DateTimeOffset RosteredFrom, DateTimeOffset RosteredTo, string State);

public sealed record OpsSpan(Guid JourneyId, string TrainNumber, DateTimeOffset From, DateTimeOffset To);

public sealed record OpsPunctualityRow(
    Guid JourneyId,
    DateOnly ServiceDate,
    string RouteCode,
    string TrainNumber,
    JourneyLifecycle Lifecycle,
    int? DelayMinutes);
