using FluentValidation;
using SmartRail.Application.Core.Messaging;

namespace SmartRail.Modules.Journeys.Application.Queries.GetBoard;

public sealed record GetBoardQuery(string StationCode, int Count, string? Language) : IQuery<BoardResponse>;

public sealed record BoardResponse(
    string TimeZone,
    DateTimeOffset AsOfUtc,
    BoardStation Station,
    IReadOnlyList<BoardItem> Items);

public sealed record BoardStation(string Code, string Name);

// ScheduledDepartureUtc never changes. ExpectedDepartureUtc is what clients show: the actual time once the train has left,
// the live estimate while live data is fresh, otherwise the scheduled time. It is never earlier than scheduled
// (REQ-LIVE-012). Confidence and EstimateSource are never null (REQ-LIVE-003). DelayMinutes is null whenever it is hidden.
// AtPlatform is true while the train has arrived at this station and not yet left.
public sealed record BoardItem(
    Guid JourneyId,
    string TrainNumber,
    string RouteCode,
    string DestinationCode,
    string DestinationName,
    DateTimeOffset ScheduledDepartureUtc,
    DateTimeOffset ExpectedDepartureUtc,
    string EstimateSource,
    int? DelayMinutes,
    string? Punctuality,
    string? Platform,
    string PlatformState,
    string Status,
    string Confidence,
    bool IsStale,
    int? AgeSeconds,
    bool AtPlatform);

public sealed class GetBoardQueryValidator : AbstractValidator<GetBoardQuery>
{
    public GetBoardQueryValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 50).WithMessage("The count must be between 1 and 50.");
    }
}
