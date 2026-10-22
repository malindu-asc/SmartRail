using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Queries.GetBoard;

public sealed class GetArrivalsQueryHandler(
    IJourneyReader reader,
    INetworkStations stations,
    INetworkTimeZone timeZone,
    TimeProvider clock,
    LiveRules rules)
    : IQueryHandler<GetArrivalsQuery, ArrivalsResponse>
{
    // The same lifetimes as the departures board: an arrived row goes after 1 minute, a cancelled one stays for 15.
    private static readonly TimeSpan ArrivedRowLifetime = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan CancelledRowLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RunningRowLifetime = TimeSpan.FromHours(2);

    private const int Spare = 10;

    public async Task<Result<ArrivalsResponse>> Handle(GetArrivalsQuery query, CancellationToken cancellationToken)
    {
        var code = query.StationCode.Trim().ToUpperInvariant();
        var now = clock.GetUtcNow();

        var today = timeZone.LocalDate(now);
        var serviceDates = new[] { today.AddDays(-1), today, today.AddDays(1) };

        var rows = await reader.GetArrivalsAsync(
            code, serviceDates, now - ArrivedRowLifetime, now - CancelledRowLifetime, now - RunningRowLifetime,
            query.Count + Spare, cancellationToken);

        var names = await stations.GetNamesAsync(rows.Select(r => r.OriginStationCode).Append(code), query.Language, cancellationToken);
        if (!names.TryGetValue(code, out var stationName))
            return JourneysErrors.StationNotFound(code);

        var items = rows
            .Select(r => ToItem(r, names, now))
            .OrderBy(i => i.ExpectedArrivalUtc)
            .ThenBy(i => i.TrainNumber, StringComparer.Ordinal)
            .Take(query.Count)
            .ToList();

        return new ArrivalsResponse(timeZone.TimeZoneId, now, new BoardStation(code, stationName), items);
    }

    private ArrivalItem ToItem(ArrivalRowReadModel r, IReadOnlyDictionary<string, string> names, DateTimeOffset now)
    {
        var status = LiveStatus.Evaluate(r.Lifecycle, r.Confidence, r.LastAcceptedUpdateAt, now, rules);

        DateTimeOffset expected;
        string source;
        if (r.ActualArrivalUtc is { } actual)
        {
            expected = actual;
            source = nameof(EstimateSource.Confirmed);
        }
        else if (status.ShowLiveTimes && r.EstimatedArrivalUtc is { } estimate)
        {
            expected = estimate;
            source = nameof(EstimateSource.Live);
        }
        else
        {
            expected = r.ScheduledArrivalUtc;
            source = nameof(EstimateSource.Scheduled);
        }

        // A train that has reached this station is Arrived, whatever the journey is doing after that.
        var publicStatus = r.ActualArrivalUtc is not null && r.Lifecycle != JourneyLifecycle.Cancelled
            ? "Arrived"
            : JourneyLifecycleRules.PublicStatus(r.Lifecycle);

        return new ArrivalItem(
            r.JourneyId,
            r.TrainNumber,
            r.RouteCode,
            r.OriginStationCode,
            names.TryGetValue(r.OriginStationCode, out var origin) ? origin : r.OriginStationCode,
            r.ScheduledArrivalUtc,
            expected,
            source,
            status.ShowLiveTimes ? r.DelayMinutes : null,
            status.ShowLiveTimes ? r.Punctuality?.ToString() : null,
            r.PlatformCode,
            r.PlatformState.ToString(),
            publicStatus,
            status.Confidence.ToString(),
            status.IsStale,
            status.AgeSeconds);
    }
}
