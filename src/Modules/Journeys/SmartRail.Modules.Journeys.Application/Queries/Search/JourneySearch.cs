using MediatR;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Contracts;

namespace SmartRail.Modules.Journeys.Application.Queries.Search;

// The contract other modules use, answered by the same search the passenger app uses.
public sealed class JourneySearch(ISender sender, IJourneyReader reader) : IJourneySearch
{
    public async Task<IReadOnlyList<JourneyOffer>> FindDirectAsync(
        string from, string to, DateTimeOffset? after, int count, string? language, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SearchJourneysQuery(from, to, after, 20, language), cancellationToken);
        if (result.IsFailure)
            return [];

        return result.Value.Options
            .Where(o => o.Transfers == 0)
            .Take(count)
            .Select(o => ToOffer(o.Legs[0], null))
            .ToList();
    }

    public async Task<JourneyOffer?> GetOfferAsync(Guid journeyId, string from, string to, string? language, CancellationToken cancellationToken)
    {
        var journey = await reader.GetJourneyAsync(journeyId, cancellationToken);
        var board = journey?.Stops.FirstOrDefault(s => s.StationCode == from.Trim().ToUpperInvariant() && s.ScheduledDepartureUtc is not null);
        if (journey is null || board?.ScheduledDepartureUtc is null)
            return null;

        // Looking a little before the scheduled departure finds this train even when it is just about to leave.
        var result = await sender.Send(
            new SearchJourneysQuery(from, to, board.ScheduledDepartureUtc.Value.AddMinutes(-1), 20, language), cancellationToken);
        if (result.IsFailure)
            return null;

        var leg = result.Value.Options.Where(o => o.Transfers == 0).Select(o => o.Legs[0]).FirstOrDefault(l => l.JourneyId == journeyId);
        return leg is null ? null : ToOffer(leg, journey.ServiceDate);
    }

    private static JourneyOffer ToOffer(SearchLeg leg, DateOnly? serviceDate) => new(
        leg.JourneyId,
        serviceDate ?? DateOnly.FromDateTime(leg.DepartureUtc.UtcDateTime),
        leg.TrainNumber, leg.RouteCode,
        leg.FromCode, leg.FromName, leg.ToCode, leg.ToName,
        leg.DepartureUtc, leg.ExpectedDepartureUtc, leg.ArrivalUtc, leg.ExpectedArrivalUtc,
        leg.Platform, leg.Status, leg.Confidence, leg.DelayMinutes);
}
