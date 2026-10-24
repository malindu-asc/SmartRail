using SmartRail.Modules.Journeys.Domain.Crew;
using SmartRail.Modules.Journeys.Domain.Journeys;

namespace SmartRail.Modules.Journeys.Application.Abstractions;

// What a driver's own duty and journeys look like, read from the roster and the time-ranged driver assignments (SPEC-011).
public sealed record DutyJourneyData(
    Guid JourneyId,
    string TrainNumber,
    string OriginCode,
    string DestinationCode,
    DateTimeOffset DepartsUtc,
    DateTimeOffset ArrivesUtc,
    JourneyLifecycle Lifecycle,
    int? DelayMinutes,
    int StopCount,
    string? FirstPlatform);

public sealed record DutyData(
    Guid DutyId,
    DateOnly ServiceDate,
    DateTimeOffset RosteredFrom,
    DateTimeOffset RosteredTo,
    DutyState State,
    DutyReviewState ReviewState,
    DateTimeOffset? SignedOnAt,
    DateTimeOffset? SignedOffAt,
    string? UnitCode,
    IReadOnlyList<DutyJourneyData> Journeys);

public sealed record DriverStopData(
    int Sequence,
    string StationCode,
    JourneyStopKind Kind,
    JourneyStopState State,
    DateTimeOffset? ScheduledArrivalUtc,
    DateTimeOffset? ScheduledDepartureUtc,
    DateTimeOffset? ExpectedArrivalUtc,
    DateTimeOffset? ExpectedDepartureUtc,
    DateTimeOffset? ActualArrivalUtc,
    DateTimeOffset? ActualDepartureUtc,
    string? Platform,
    string? OriginalPlatform);

public sealed record DriverJourneyData(
    Guid JourneyId,
    string TrainNumber,
    string OriginCode,
    string DestinationCode,
    JourneyLifecycle Lifecycle,
    int? DelayMinutes,
    string? DelayReasonCode,
    Guid DutyId,
    DutyState DutyState,
    string? UnitCode,
    IReadOnlyList<DriverStopData> Stops);

public interface IDriverStore
{
    // The duty tracked for update, whoever owns it (the handler checks the driver).
    Task<CrewDuty?> GetDutyAsync(Guid dutyId, CancellationToken cancellationToken);

    // The driver's duties between two service dates, with their journeys, oldest first.
    Task<IReadOnlyList<DutyData>> ListDutiesAsync(string driverSubject, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    // One journey as the driver sees it, only if a duty of theirs is assigned to it. Null otherwise.
    Task<DriverJourneyData?> GetJourneyAsync(Guid journeyId, string driverSubject, DateTimeOffset now, CancellationToken cancellationToken);
}
