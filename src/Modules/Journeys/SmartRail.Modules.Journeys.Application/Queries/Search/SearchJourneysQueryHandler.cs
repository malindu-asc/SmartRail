using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Queries.Search;

public sealed class SearchJourneysQueryHandler(
    IJourneyReader reader,
    INetworkStations stations,
    INetworkTimeZone timeZone,
    TimeProvider clock,
    LiveRules rules)
    : IQueryHandler<SearchJourneysQuery, SearchResponse>
{
    // ASM-025: the minimum time to change trains, at every station.
    public static readonly TimeSpan MinimumTransfer = TimeSpan.FromMinutes(5);

    public async Task<Result<SearchResponse>> Handle(SearchJourneysQuery query, CancellationToken cancellationToken)
    {
        var from = query.From.Trim().ToUpperInvariant();
        var to = query.To.Trim().ToUpperInvariant();
        if (from == to)
            return JourneysErrors.SameStation;

        var now = clock.GetUtcNow();
        var after = query.After ?? now;
        var today = timeZone.LocalDate(now);
        var dates = new[] { today.AddDays(-1), today, today.AddDays(1) };

        var names = await stations.GetNamesAsync([from, to], query.Language, cancellationToken);
        if (!names.ContainsKey(from)) return JourneysErrors.StationNotFound(from);
        if (!names.ContainsKey(to)) return JourneysErrors.StationNotFound(to);

        var journeys = await reader.GetSearchCandidatesAsync(from, to, dates, after, cancellationToken);
        var views = journeys
            .Select(j => new View(j, LiveStatus.Evaluate(j.Lifecycle, j.Confidence, j.LastAcceptedUpdateAt, now, rules)))
            .ToList();

        var options = new List<Candidate>();

        // Direct.
        foreach (var v in views)
        {
            if (Ride(v, from, to, after) is { } leg)
                options.Add(new Candidate([leg]));
        }

        // One transfer: first train from `from` to X, second train from X to `to`.
        foreach (var first in views)
        {
            foreach (var board in Boardings(first, from, after))
            {
                foreach (var change in Alightings(first, board))
                {
                    if (change.StationCode == to) continue;

                    foreach (var second in views)
                    {
                        if (second.Journey.Id == first.Journey.Id) continue;
                        if (Ride(second, change.StationCode, to, null) is not { } leg2) continue;

                        var leg1 = MakeLeg(first, board, change);
                        // The connection is judged on scheduled times; a late first train only makes it "at risk".
                        if (leg2.Departure - leg1.Arrival < MinimumTransfer) continue;

                        options.Add(new Candidate([leg1, leg2]));
                    }
                }
            }
        }

        // A change that gets there no sooner than a direct train that leaves no earlier is pointless.
        var directs = options.Where(o => o.Legs.Count == 1).ToList();
        options = options
            .Where(o => o.Legs.Count == 1 || !directs.Any(d => d.Legs[0].Departure >= o.Legs[0].Departure && d.Legs[0].Arrival <= o.Legs[1].Arrival))
            .OrderBy(o => o.Legs[^1].Arrival)
            .ThenBy(o => o.Legs.Count)
            .ThenBy(o => o.Legs[0].Departure)
            .DistinctBy(o => string.Join("|", o.Legs.Select(l => l.Journey.Id)))
            .Take(query.Count)
            .ToList();

        var allCodes = options.SelectMany(o => o.Legs).SelectMany(l => new[] { l.FromCode, l.ToCode }).Concat([from, to]);
        var allNames = await stations.GetNamesAsync(allCodes, query.Language, cancellationToken);
        string Name(string code) => allNames.TryGetValue(code, out var n) ? n : code;

        var result = options.Select(o =>
        {
            var legs = o.Legs.Select(l => new SearchLeg(
                l.Journey.Id, l.Journey.TrainNumber, l.Journey.RouteCode,
                l.FromCode, Name(l.FromCode), l.ToCode, Name(l.ToCode),
                l.Departure, l.ExpectedDeparture, l.Arrival, l.ExpectedArrival,
                l.Platform, l.PlatformState, JourneyLifecycleRules.PublicStatus(l.Journey.Lifecycle),
                l.Status.Confidence.ToString(), l.Status.ShowLiveTimes ? l.Journey.DelayMinutes : null)).ToList();

            SearchTransfer? transfer = null;
            if (o.Legs.Count == 2)
            {
                var wait = o.Legs[1].ExpectedDeparture - o.Legs[0].ExpectedArrival;
                transfer = new SearchTransfer(o.Legs[0].ToCode, Name(o.Legs[0].ToCode), (int)Math.Floor(wait.TotalMinutes), wait < MinimumTransfer);
            }

            var firstLeg = o.Legs[0];
            var lastLeg = o.Legs[^1];
            return new SearchOption(
                o.Legs.Count - 1, firstLeg.Departure, firstLeg.ExpectedDeparture, lastLeg.Arrival, lastLeg.ExpectedArrival,
                (int)Math.Round((lastLeg.ExpectedArrival - firstLeg.ExpectedDeparture).TotalMinutes), transfer, legs);
        }).ToList();

        return new SearchResponse(timeZone.TimeZoneId, now, new SearchStation(from, Name(from)), new SearchStation(to, Name(to)), result);
    }

    private sealed record View(JourneyReadModel Journey, LiveStatus Status);

    private sealed record Candidate(IReadOnlyList<Leg> Legs);

    private sealed record Leg(
        JourneyReadModel Journey, LiveStatus Status, string FromCode, string ToCode,
        DateTimeOffset Departure, DateTimeOffset ExpectedDeparture, DateTimeOffset Arrival, DateTimeOffset ExpectedArrival,
        string? Platform, string PlatformState);

    private static bool CanBoard(JourneyStopReadModel s) =>
        s.Kind is JourneyStopKind.Stop or JourneyStopKind.PickUpOnly && s.ScheduledDepartureUtc is not null;

    private static bool CanAlight(JourneyStopReadModel s) =>
        s.Kind is JourneyStopKind.Stop or JourneyStopKind.SetDownOnly && s.ScheduledArrivalUtc is not null;

    private static IEnumerable<JourneyStopReadModel> Boardings(View v, string station, DateTimeOffset after) =>
        v.Journey.Stops.Where(s => s.StationCode == station && CanBoard(s) && s.State != JourneyStopState.Departed && s.ScheduledDepartureUtc >= after);

    private static IEnumerable<JourneyStopReadModel> Alightings(View v, JourneyStopReadModel board) =>
        v.Journey.Stops.Where(s => s.Sequence > board.Sequence && CanAlight(s));

    private static Leg MakeLeg(View v, JourneyStopReadModel board, JourneyStopReadModel alight)
    {
        var live = v.Status.ShowLiveTimes;
        var departure = board.ScheduledDepartureUtc!.Value;
        var arrival = alight.ScheduledArrivalUtc!.Value;

        var expectedDeparture = board.ActualDepartureUtc ?? (live ? board.EstimatedDepartureUtc : null) ?? departure;
        var expectedArrival = alight.ActualArrivalUtc ?? (live ? alight.EstimatedArrivalUtc : null) ?? arrival;
        if (expectedDeparture < departure) expectedDeparture = departure;
        if (expectedArrival < arrival) expectedArrival = arrival;

        return new Leg(v.Journey, v.Status, board.StationCode, alight.StationCode, departure, expectedDeparture, arrival, expectedArrival,
            board.PlatformCode, board.PlatformState.ToString());
    }

    // The first boarding at `from` that has a later alighting at `to`.
    private static Leg? Ride(View v, string from, string to, DateTimeOffset? after)
    {
        foreach (var board in v.Journey.Stops.Where(s => s.StationCode == from && CanBoard(s) && s.State != JourneyStopState.Departed))
        {
            if (after is { } a && board.ScheduledDepartureUtc < a) continue;
            var alight = v.Journey.Stops.FirstOrDefault(s => s.Sequence > board.Sequence && s.StationCode == to && CanAlight(s));
            if (alight is not null) return MakeLeg(v, board, alight);
        }

        return null;
    }
}
