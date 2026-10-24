using FluentValidation;
using SmartRail.Application.Core.Messaging;

namespace SmartRail.Modules.Journeys.Application.Queries.GetBoard;

// The arrivals board of a station: the trains coming in, with where each one started. It follows the same rules as the departures
// board (the live estimate while it is fresh, otherwise the scheduled time; never earlier than scheduled).
public sealed record GetArrivalsQuery(string StationCode, int Count, string? Language) : IQuery<ArrivalsResponse>;

public sealed record ArrivalsResponse(
    string TimeZone,
    DateTimeOffset AsOfUtc,
    BoardStation Station,
    IReadOnlyList<ArrivalItem> Items);

// ScheduledArrivalUtc never changes. ExpectedArrivalUtc is the actual time once the train has arrived, the live estimate while live
// data is fresh, otherwise the scheduled time. Status is Planned, Running, Cancelled or Arrived.
public sealed record ArrivalItem(
    Guid JourneyId,
    string TrainNumber,
    string RouteCode,
    string OriginCode,
    string OriginName,
    DateTimeOffset ScheduledArrivalUtc,
    DateTimeOffset ExpectedArrivalUtc,
    string EstimateSource,
    int? DelayMinutes,
    string? Punctuality,
    string? Platform,
    string PlatformState,
    string Status,
    string Confidence,
    bool IsStale,
    int? AgeSeconds);

public sealed class GetArrivalsQueryValidator : AbstractValidator<GetArrivalsQuery>
{
    public GetArrivalsQueryValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 50).WithMessage("The count must be between 1 and 50.");
    }
}
