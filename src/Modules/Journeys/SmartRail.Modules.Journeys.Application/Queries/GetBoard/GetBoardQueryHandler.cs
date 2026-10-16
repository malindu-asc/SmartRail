using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Queries.GetBoard;

public sealed class GetBoardQueryHandler(
    IJourneyReader reader,
    INetworkStations stations,
    INetworkTimeZone timeZone,
    TimeProvider clock)
    : IQueryHandler<GetBoardQuery, BoardResponse>
{
    // REQ-DSP-007: a departed row disappears after 1 minute; a cancelled row stays for 15 minutes.
    // Until live data exists (SPEC-005) "departed" means "the scheduled departure has passed".
    private static readonly TimeSpan DepartedRowLifetime = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan CancelledRowLifetime = TimeSpan.FromMinutes(15);

    public async Task<Result<BoardResponse>> Handle(GetBoardQuery query, CancellationToken cancellationToken)
    {
        var code = query.StationCode.Trim().ToUpperInvariant();
        var now = clock.GetUtcNow();

        // Yesterday's service date is included so a post-midnight departure of last night's service still appears (REQ-LIVE-009).
        var today = timeZone.LocalDate(now);
        var serviceDates = new[] { today.AddDays(-1), today, today.AddDays(1) };

        var rows = await reader.GetBoardAsync(
            code, serviceDates, now - DepartedRowLifetime, now - CancelledRowLifetime, query.Count, cancellationToken);

        var names = await stations.GetNamesAsync(rows.Select(r => r.DestinationStationCode).Append(code), query.Language, cancellationToken);
        if (!names.TryGetValue(code, out var stationName))
            return JourneysErrors.StationNotFound(code);

        var items = rows.Select(r => new BoardItem(
            r.JourneyId,
            r.TrainNumber,
            r.RouteCode,
            r.DestinationStationCode,
            names.TryGetValue(r.DestinationStationCode, out var destination) ? destination : r.DestinationStationCode,
            r.ScheduledDepartureUtc,
            r.PlatformCode,
            r.PlatformState.ToString(),
            JourneyLifecycleRules.PublicStatus(r.Lifecycle),
            r.Confidence.ToString())).ToList();

        return new BoardResponse(timeZone.TimeZoneId, now, new BoardStation(code, stationName), items);
    }
}
