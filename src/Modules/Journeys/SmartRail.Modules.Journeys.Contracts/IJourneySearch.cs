namespace SmartRail.Modules.Journeys.Contracts;

// Direct trains between two stations, for modules that sell or follow them (tickets). Times are what clients are told:
// the Expected times follow the live rules of the board.
public interface IJourneySearch
{
    // Direct trains from `from` to `to` leaving at or after `after` (null means now), soonest first.
    Task<IReadOnlyList<JourneyOffer>> FindDirectAsync(string from, string to, DateTimeOffset? after, int count, string? language, CancellationToken cancellationToken);

    // One train between two of its stops, or null when that train does not run between them.
    Task<JourneyOffer?> GetOfferAsync(Guid journeyId, string from, string to, string? language, CancellationToken cancellationToken);
}

public sealed record JourneyOffer(
    Guid JourneyId,
    DateOnly ServiceDate,
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
    string Status,
    string Confidence,
    int? DelayMinutes);
