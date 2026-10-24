using SmartRail.Application.Core.Messaging;

namespace SmartRail.Modules.Journeys.Application.Queries.GetJourney;

public sealed record GetJourneyQuery(Guid JourneyId, string? Language) : IQuery<JourneyResponse>;

// Status is "Planned" for both Planned and Ready (state-models.md). Confidence is never null (REQ-LIVE-003):
// Scheduled until live data arrives, Live while fresh (IsStale from 60 s, with its age), Unknown after 300 s, Confirmed when
// it has finished. DelayMinutes and Punctuality are null whenever they are hidden (Unknown, Interrupted, not yet running).
public sealed record JourneyResponse(
    Guid JourneyId,
    string ServiceDate,
    string TimeZone,
    string TrainNumber,
    string RouteCode,
    string Status,
    string Confidence,
    JourneyEnd Origin,
    JourneyEnd Destination,
    int? DelayMinutes,
    string? Punctuality,
    bool IsStale,
    int? AgeSeconds,
    DateTimeOffset? AsOfUtc,
    IReadOnlyList<JourneyStopResponse> Stops);

public sealed record JourneyEnd(string Code, string Name);

// EstimatedArrivalUtc and EstimatedDepartureUtc are null unless live data is fresh; the scheduled times never change.
// EstimateSource says what the stop's times rest on: Confirmed (it happened), Live (an estimate) or Scheduled.
public sealed record JourneyStopResponse(
    int Sequence,
    string StationCode,
    string StationName,
    string State,
    DateTimeOffset? ScheduledArrivalUtc,
    DateTimeOffset? ScheduledDepartureUtc,
    DateTimeOffset? EstimatedArrivalUtc,
    DateTimeOffset? EstimatedDepartureUtc,
    DateTimeOffset? ActualArrivalUtc,
    DateTimeOffset? ActualDepartureUtc,
    string EstimateSource,
    string? Platform,
    string PlatformState);
