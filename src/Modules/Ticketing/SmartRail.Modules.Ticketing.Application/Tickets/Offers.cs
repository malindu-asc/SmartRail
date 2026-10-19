using FluentValidation;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Contracts;
using SmartRail.Modules.Network.Contracts;
using SmartRail.Modules.Ticketing.Application.Abstractions;
using SmartRail.Modules.Ticketing.Domain.Bookings;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Ticketing.Application.Tickets;

// ---- Choose ticket: the direct trains with their fare and whether a seat is needed ----

public sealed record GetOffersQuery(string From, string To, DateTimeOffset? After, int Adults, int Children, int Seniors, string? Language)
    : IQuery<OffersResponse>;

public sealed record OffersResponse(string TimeZone, string FromName, string ToName, IReadOnlyList<TicketOffer> Offers);

// Fare is shown for information; nothing is charged in this version.
public sealed record TicketOffer(
    Guid JourneyId,
    string TrainNumber,
    string FromCode,
    string ToCode,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ExpectedDepartureUtc,
    DateTimeOffset ArrivalUtc,
    DateTimeOffset ExpectedArrivalUtc,
    int DurationMinutes,
    string? Platform,
    string Status,
    string Confidence,
    int? DelayMinutes,
    bool SeatRequired,
    FareQuote Fare);

public sealed class GetOffersQueryValidator : AbstractValidator<GetOffersQuery>
{
    public GetOffersQueryValidator()
    {
        RuleFor(x => x.From).NotEmpty();
        RuleFor(x => x.To).NotEmpty().NotEqual(x => x.From).WithMessage("Choose two different stations.");
        RuleFor(x => x.Adults + x.Children + x.Seniors).InclusiveBetween(1, 9).WithMessage("Choose between 1 and 9 travellers.");
    }
}

public sealed class GetOffersQueryHandler(
    IJourneySearch search,
    INetworkStations stations,
    INetworkTimeZone timeZone,
    TicketingOptions options,
    FareOptions fares)
    : IQueryHandler<GetOffersQuery, OffersResponse>
{
    public async Task<Result<OffersResponse>> Handle(GetOffersQuery query, CancellationToken cancellationToken)
    {
        var from = query.From.Trim().ToUpperInvariant();
        var to = query.To.Trim().ToUpperInvariant();

        var names = await stations.GetNamesAsync([from, to], query.Language, cancellationToken);
        var offers = await search.FindDirectAsync(from, to, query.After, 10, query.Language, cancellationToken);

        var items = offers.Select(o => new TicketOffer(
            o.JourneyId, o.TrainNumber, o.FromCode, o.ToCode,
            o.DepartureUtc, o.ExpectedDepartureUtc, o.ArrivalUtc, o.ExpectedArrivalUtc,
            (int)Math.Round((o.ArrivalUtc - o.DepartureUtc).TotalMinutes),
            o.Platform, o.Status, o.Confidence, o.DelayMinutes,
            options.SeatReservedRoutes.Contains(o.RouteCode, StringComparer.OrdinalIgnoreCase),
            FareCalculator.Quote(fares, (int)Math.Round((o.ArrivalUtc - o.DepartureUtc).TotalMinutes), query.Adults, query.Children, query.Seniors))).ToList();

        return new OffersResponse(
            timeZone.TimeZoneId,
            names.GetValueOrDefault(from, from),
            names.GetValueOrDefault(to, to),
            items);
    }
}
