using FluentValidation;
using SmartRail.Application.Core.Messaging;

namespace SmartRail.Modules.Journeys.Application.Queries.Search;

// After is the earliest departure (UTC); null means now.
public sealed record SearchJourneysQuery(string From, string To, DateTimeOffset? After, int Count, string? Language) : IQuery<SearchResponse>;

public sealed record SearchResponse(
    string TimeZone,
    DateTimeOffset AsOfUtc,
    SearchStation From,
    SearchStation To,
    IReadOnlyList<SearchOption> Options);

public sealed record SearchStation(string Code, string Name);

// Transfers is 0 or 1. The scheduled times never change; Expected times follow the live rules of the board.
public sealed record SearchOption(
    int Transfers,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ExpectedDepartureUtc,
    DateTimeOffset ArrivalUtc,
    DateTimeOffset ExpectedArrivalUtc,
    int DurationMinutes,
    SearchTransfer? Transfer,
    IReadOnlyList<SearchLeg> Legs);

// ConnectionAtRisk: the expected wait is shorter than the minimum transfer time (REQ-PAX-006).
public sealed record SearchTransfer(string StationCode, string StationName, int WaitMinutes, bool ConnectionAtRisk);

public sealed record SearchLeg(
    Guid JourneyId,
    string TrainNumber,
    string RouteCode,
    string FromCode,
    string FromName,
    string ToCode,
    string ToName,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ExpectedDepartureUtc,
    DateTimeOffset ArrivalUtc,
    DateTimeOffset ExpectedArrivalUtc,
    string? Platform,
    string PlatformState,
    string Status,
    string Confidence,
    int? DelayMinutes);

public sealed class SearchJourneysQueryValidator : AbstractValidator<SearchJourneysQuery>
{
    public SearchJourneysQueryValidator()
    {
        RuleFor(x => x.From).NotEmpty().WithMessage("Choose where you start.");
        RuleFor(x => x.To).NotEmpty().WithMessage("Choose where you are going.");
        RuleFor(x => x.Count).InclusiveBetween(1, 20).WithMessage("The count must be between 1 and 20.");
    }
}
