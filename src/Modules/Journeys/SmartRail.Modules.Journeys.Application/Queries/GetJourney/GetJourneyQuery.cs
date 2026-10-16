using SmartRail.Application.Core.Messaging;

namespace SmartRail.Modules.Journeys.Application.Queries.GetJourney;

public sealed record GetJourneyQuery(Guid JourneyId, string? Language) : IQuery<JourneyResponse>;

// Status is "Planned" for both Planned and Ready (state-models.md). Delay and estimates are null until live data arrives.
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
    IReadOnlyList<JourneyStopResponse> Stops);

public sealed record JourneyEnd(string Code, string Name);

public sealed record JourneyStopResponse(
    int Sequence,
    string StationCode,
    string StationName,
    string State,
    DateTimeOffset? ScheduledArrivalUtc,
    DateTimeOffset? ScheduledDepartureUtc,
    string? Platform,
    string PlatformState);
