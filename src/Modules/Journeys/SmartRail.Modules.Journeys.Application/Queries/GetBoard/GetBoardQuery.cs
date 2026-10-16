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

public sealed record BoardItem(
    Guid JourneyId,
    string TrainNumber,
    string RouteCode,
    string DestinationCode,
    string DestinationName,
    DateTimeOffset ScheduledDepartureUtc,
    string? Platform,
    string PlatformState,
    string Status,
    string Confidence);

public sealed class GetBoardQueryValidator : AbstractValidator<GetBoardQuery>
{
    public GetBoardQueryValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 50).WithMessage("The count must be between 1 and 50.");
    }
}
