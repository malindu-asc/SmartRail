using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Queries.GetBoard;

public sealed class GetBoardQueryHandler(
    IJourneyReader reader,
    INetworkStations stations,
    INetworkTimeZone timeZone,
    TimeProvider clock,
    LiveRules rules)
    : IQueryHandler<GetBoardQuery, BoardResponse>
{
    // REQ-DSP-007: a departed row disappears after 1 minute; a cancelled row stays for 15 minutes.
    private static readonly TimeSpan DepartedRowLifetime = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan CancelledRowLifetime = TimeSpan.FromMinutes(15);

    // A running train that has not reached this station yet stays on the board, but not for ever if its data is lost.
    private static readonly TimeSpan RunningRowLifetime = TimeSpan.FromHours(2);

    // The page is read with a little spare, then sorted again with the times clients are actually told (an Unknown journey
    // goes back to its scheduled time, which may move it up).
    private const int Spare = 10;

    public async Task<Result<BoardResponse>> Handle(GetBoardQuery query, CancellationToken cancellationToken)
    {
        var code = query.StationCode.Trim().ToUpperInvariant();
        var now = clock.GetUtcNow();

        // Yesterday's service date is included so a post-midnight departure of last night's service still appears (REQ-LIVE-009).
        var today = timeZone.LocalDate(now);
        var serviceDates = new[] { today.AddDays(-1), today, today.AddDays(1) };

        var rows = await reader.GetBoardAsync(
            code, serviceDates, now - DepartedRowLifetime, now - CancelledRowLifetime, now - RunningRowLifetime,
            query.Count + Spare, cancellationToken);

        var names = await stations.GetNamesAsync(rows.Select(r => r.DestinationStationCode).Append(code), query.Language, cancellationToken);
        if (!names.TryGetValue(code, out var stationName))
            return JourneysErrors.StationNotFound(code);

        var items = rows
            .Select(r => ToItem(r, names, now))
            .OrderBy(i => i.ExpectedDepartureUtc)
            .ThenBy(i => i.TrainNumber, StringComparer.Ordinal)
            .Take(query.Count)
            .ToList();

        return new BoardResponse(timeZone.TimeZoneId, now, new BoardStation(code, stationName), items);
    }

    private BoardItem ToItem(BoardRowReadModel r, IReadOnlyDictionary<string, string> names, DateTimeOffset now)
    {
        var status = LiveStatus.Evaluate(r.Lifecycle, r.Confidence, r.LastAcceptedUpdateAt, now, rules);

        DateTimeOffset expected;
        string source;
        if (r.ActualDepartureUtc is { } actual)
        {
            expected = actual;
            source = nameof(EstimateSource.Confirmed);
        }
        else if (status.ShowLiveTimes && r.EstimatedDepartureUtc is { } estimate)
        {
            expected = estimate;
            source = nameof(EstimateSource.Live);
        }
        else
        {
            expected = r.ScheduledDepartureUtc;
            source = nameof(EstimateSource.Scheduled);
        }

        return new BoardItem(
            r.JourneyId,
            r.TrainNumber,
            r.RouteCode,
            r.DestinationStationCode,
            names.TryGetValue(r.DestinationStationCode, out var destination) ? destination : r.DestinationStationCode,
            r.ScheduledDepartureUtc,
            expected,
            source,
            status.ShowLiveTimes ? r.DelayMinutes : null,
            status.ShowLiveTimes ? r.Punctuality?.ToString() : null,
            r.PlatformCode,
            r.PlatformState.ToString(),
            JourneyLifecycleRules.PublicStatus(r.Lifecycle),
            status.Confidence.ToString(),
            status.IsStale,
            status.AgeSeconds);
    }
}
