using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Contracts;
using SmartRail.Modules.Journeys.Domain.Journeys;

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence;

internal sealed class JourneyReader(JourneysDbContext context) : IJourneyReader, IJourneyLookup
{
    public async Task<JourneyReadModel?> GetJourneyAsync(Guid journeyId, CancellationToken cancellationToken)
    {
        var journey = await context.Journeys
            .AsNoTracking()
            .Include(j => j.Stops)
            .AsSplitQuery()
            .FirstOrDefaultAsync(j => j.Id == journeyId, cancellationToken);

        return journey is null
            ? null
            : new JourneyReadModel(
                journey.Id,
                journey.ServiceDate,
                journey.TrainNumber,
                journey.RouteCode,
                journey.OriginStationCode,
                journey.DestinationStationCode,
                journey.Lifecycle,
                journey.Confidence,
                journey.DelayMinutes,
                journey.PunctualityBand,
                journey.LastAcceptedUpdateAt,
                journey.Stops.OrderBy(s => s.Sequence).Select(s => new JourneyStopReadModel(
                    s.Sequence, s.StationCode, s.Kind, s.State,
                    s.ScheduledArrivalUtc, s.ScheduledDepartureUtc, s.EstimatedArrivalUtc, s.EstimatedDepartureUtc,
                    s.ActualArrivalUtc, s.ActualDepartureUtc, s.PlatformCode, s.PlatformState)).ToList());
    }

    public async Task<IReadOnlyList<BoardRowReadModel>> GetBoardAsync(
        string stationCode,
        IReadOnlyCollection<DateOnly> serviceDates,
        DateTimeOffset notBeforeUtc,
        DateTimeOffset cancelledNotBeforeUtc,
        DateTimeOffset runningNotBeforeUtc,
        int count,
        CancellationToken cancellationToken)
    {
        var dates = serviceDates.ToArray();

        var rows = await context.Journeys
            .AsNoTracking()
            .Where(j => dates.Contains(j.ServiceDate) && j.Lifecycle != JourneyLifecycle.Completed)
            .SelectMany(j => j.Stops, (j, s) => new { Journey = j, Stop = s })
            .Where(x => x.Stop.StationCode == stationCode
                        && x.Stop.Kind != JourneyStopKind.Passing
                        && x.Stop.Kind != JourneyStopKind.SetDownOnly
                        && x.Stop.ScheduledDepartureUtc != null
                        && (x.Journey.Lifecycle == JourneyLifecycle.Cancelled
                            ? x.Stop.ScheduledDepartureUtc >= cancelledNotBeforeUtc
                            : (x.Stop.ScheduledDepartureUtc >= notBeforeUtc
                               || (x.Stop.ActualDepartureUtc != null && x.Stop.ActualDepartureUtc >= notBeforeUtc)
                               || (x.Journey.Lifecycle == JourneyLifecycle.Running
                                   && x.Stop.State != JourneyStopState.Departed
                                   && x.Stop.ScheduledDepartureUtc >= runningNotBeforeUtc))))
            .OrderBy(x => x.Stop.EstimatedDepartureUtc ?? x.Stop.ScheduledDepartureUtc)
            .ThenBy(x => x.Journey.TrainNumber)
            .Take(count)
            .Select(x => new BoardRowReadModel(
                x.Journey.Id,
                x.Journey.TrainNumber,
                x.Journey.RouteCode,
                x.Journey.DestinationStationCode,
                x.Stop.ScheduledDepartureUtc!.Value,
                x.Stop.EstimatedDepartureUtc,
                x.Stop.ActualDepartureUtc,
                x.Stop.PlatformCode,
                x.Stop.PlatformState,
                x.Journey.Lifecycle,
                x.Journey.Confidence,
                x.Journey.DelayMinutes,
                x.Journey.PunctualityBand,
                x.Journey.LastAcceptedUpdateAt))
            .ToListAsync(cancellationToken);

        return rows;
    }

    // IJourneyLookup: what other modules may know about a journey.
    public async Task<JourneyRef?> FindAsync(Guid journeyId, CancellationToken cancellationToken)
    {
        var journey = await context.Journeys
            .AsNoTracking()
            .Include(j => j.Stops)
            .AsSplitQuery()
            .FirstOrDefaultAsync(j => j.Id == journeyId, cancellationToken);

        return journey is null
            ? null
            : new JourneyRef(
                journey.Id,
                journey.ServiceDate,
                journey.Lifecycle.ToString(),
                journey.Stops.OrderBy(s => s.Sequence).Select(s => s.StationCode).ToList());
    }
}
