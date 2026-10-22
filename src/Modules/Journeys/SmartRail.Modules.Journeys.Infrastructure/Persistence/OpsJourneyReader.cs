using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Crew;
using SmartRail.Modules.Journeys.Domain.Fleet;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence;

internal sealed class OpsJourneyReader(JourneysDbContext context, TimeProvider clock) : IOpsJourneyReader
{
    private static readonly JourneyLifecycle[] Active =
        [JourneyLifecycle.Planned, JourneyLifecycle.Ready, JourneyLifecycle.Running, JourneyLifecycle.Interrupted];

    private static readonly JourneyLifecycle[] Finished =
        [JourneyLifecycle.Completed, JourneyLifecycle.Cancelled, JourneyLifecycle.TerminatedShort, JourneyLifecycle.TerminatedEmergency];

    public async Task<IReadOnlyList<OpsJourneyData>> ListAsync(OpsJourneyFilter filter, CancellationToken cancellationToken)
    {
        var query = context.Journeys.AsNoTracking().AsSplitQuery()
            .Include(j => j.Stops).Include(j => j.VehicleAssignments).Include(j => j.DriverAssignments)
            .Where(j => j.ServiceDate == filter.ServiceDate);

        if (!string.IsNullOrWhiteSpace(filter.RouteCode))
            query = query.Where(j => j.RouteCode == filter.RouteCode);
        if (!string.IsNullOrWhiteSpace(filter.StationCode))
            query = query.Where(j => j.Stops.Any(s => s.StationCode == filter.StationCode));
        if (!string.IsNullOrWhiteSpace(filter.Text))
        {
            var text = filter.Text.Trim();
            query = query.Where(j => j.TrainNumber.Contains(text));
        }

        var journeys = await query.OrderBy(j => j.ScheduledDepartureUtc).ThenBy(j => j.TrainNumber).ToListAsync(cancellationToken);
        return await MapAsync(journeys, cancellationToken);
    }

    public async Task<OpsJourneyData?> GetAsync(Guid journeyId, CancellationToken cancellationToken)
    {
        var journey = await context.Journeys.AsNoTracking().AsSplitQuery()
            .Include(j => j.Stops).Include(j => j.VehicleAssignments).Include(j => j.DriverAssignments)
            .FirstOrDefaultAsync(j => j.Id == journeyId, cancellationToken);

        return journey is null ? null : (await MapAsync([journey], cancellationToken))[0];
    }

    public async Task<IReadOnlyList<OpsEventData>> GetEventsAsync(Guid journeyId, int take, CancellationToken cancellationToken) =>
        await context.JourneyEvents.AsNoTracking().Where(e => e.JourneyId == journeyId)
            .OrderByDescending(e => e.ReceivedAt).Take(take)
            .Select(e => new OpsEventData(e.Id, e.StopSequence, e.EventType, e.SourceKind, e.OccurredAtSource, e.ReceivedAt, e.Disposition, e.DispositionReason))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OpsPlatformUse>> GetPlatformUseAsync(
        string stationCode, string platformCode, DateTimeOffset from, DateTimeOffset to, Guid excludeJourneyId, CancellationToken cancellationToken)
    {
        var journeys = await context.Journeys.AsNoTracking().Include(j => j.Stops)
            .Where(j => j.Id != excludeJourneyId && Active.Contains(j.Lifecycle)
                && j.Stops.Any(s => s.StationCode == stationCode && s.PlatformCode == platformCode && s.Kind != JourneyStopKind.Passing
                    && (s.ScheduledArrivalUtc ?? s.ScheduledDepartureUtc) <= to
                    && (s.EstimatedDepartureUtc ?? s.ScheduledDepartureUtc ?? s.ScheduledArrivalUtc) >= from))
            .ToListAsync(cancellationToken);

        return journeys.Select(j =>
        {
            var stop = j.Stops.First(s => s.StationCode == stationCode && s.PlatformCode == platformCode);
            var start = stop.ScheduledArrivalUtc ?? stop.ScheduledDepartureUtc ?? j.ScheduledDepartureUtc;
            var end = stop.EstimatedDepartureUtc ?? stop.ScheduledDepartureUtc ?? start;
            return new OpsPlatformUse(j.Id, j.TrainNumber, j.DestinationStationCode, start, end);
        }).OrderBy(u => u.From).ToList();
    }

    public async Task<IReadOnlyList<OpsVehicleData>> ListVehiclesAsync(CancellationToken cancellationToken) =>
        await context.Vehicles.AsNoTracking().OrderBy(v => v.Code)
            .Select(v => new OpsVehicleData(v.Id, v.Code, v.Name, v.Kind.ToString(), v.IsActive)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OpsUnavailability>> ListUnavailabilityAsync(Guid? vehicleId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        await context.VehicleUnavailabilities.AsNoTracking()
            .Where(u => u.EndedAt == null && (vehicleId == null || u.VehicleId == vehicleId) && u.From < to && (u.To == null || u.To > from))
            .OrderBy(u => u.From)
            .Select(u => new OpsUnavailability(u.Id, u.VehicleId, u.From, u.To, u.Reason, u.CreatedBy, u.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OpsDutyData>> ListDutiesAsync(DateOnly serviceDate, CancellationToken cancellationToken) =>
        await context.CrewDuties.AsNoTracking().Where(d => d.ServiceDate == serviceDate).OrderBy(d => d.RosteredFrom)
            .Select(d => new OpsDutyData(d.Id, d.DriverSubject, d.ServiceDate, d.RosteredFrom, d.RosteredTo, d.State.ToString()))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OpsSpan>> ListVehicleSpansAsync(
        Guid vehicleId, DateTimeOffset from, DateTimeOffset to, Guid? exceptJourneyId, CancellationToken cancellationToken) =>
        await context.Journeys.AsNoTracking()
            .Where(j => j.Id != exceptJourneyId && Active.Contains(j.Lifecycle)
                && j.VehicleAssignments.Any(a => a.VehicleId == vehicleId && a.ValidFrom < to && (a.ValidTo == null || a.ValidTo > from)))
            .Select(j => new OpsSpan(j.Id, j.TrainNumber, j.ScheduledDepartureUtc, j.ScheduledArrivalUtc))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OpsSpan>> ListDutySpansAsync(Guid dutyId, Guid? exceptJourneyId, CancellationToken cancellationToken) =>
        await context.Journeys.AsNoTracking()
            .Where(j => j.Id != exceptJourneyId && Active.Contains(j.Lifecycle) && j.DriverAssignments.Any(a => a.DutyId == dutyId))
            .Select(j => new OpsSpan(j.Id, j.TrainNumber, j.ScheduledDepartureUtc, j.ScheduledArrivalUtc))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OpsPunctualityRow>> ListPunctualityAsync(DateOnly from, DateOnly to, string? routeCode, CancellationToken cancellationToken)
    {
        var query = context.Journeys.AsNoTracking().Where(j => j.ServiceDate >= from && j.ServiceDate <= to && Finished.Contains(j.Lifecycle));
        if (!string.IsNullOrWhiteSpace(routeCode))
            query = query.Where(j => j.RouteCode == routeCode);

        return await query.OrderBy(j => j.ServiceDate).ThenBy(j => j.ScheduledDepartureUtc)
            .Select(j => new OpsPunctualityRow(j.Id, j.ServiceDate, j.RouteCode, j.TrainNumber, j.Lifecycle, j.DelayMinutes))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountCorrectedEventsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        context.JourneyEvents.AsNoTracking()
            .CountAsync(e => e.ReceivedAt >= from && e.ReceivedAt < to && (e.SourceKind == EventSourceKind.Controller || e.Disposition == EventDisposition.Superseded), cancellationToken);

    // The vehicle and duty in force "now" (for a journey that has not left, the ones in force at its departure).
    private async Task<IReadOnlyList<OpsJourneyData>> MapAsync(IReadOnlyList<Journey> journeys, CancellationToken cancellationToken)
    {
        if (journeys.Count == 0)
            return [];

        var now = clock.GetUtcNow();
        var vehicleIds = journeys.SelectMany(j => j.VehicleAssignments.Select(a => a.VehicleId)).Distinct().ToList();
        var dutyIds = journeys.SelectMany(j => j.DriverAssignments.Select(a => a.DutyId)).Distinct().ToList();
        var vehicles = await context.Vehicles.AsNoTracking().Where(v => vehicleIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v.Code, cancellationToken);
        var duties = await context.CrewDuties.AsNoTracking().Where(d => dutyIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.DriverSubject, cancellationToken);

        return journeys.Select(j =>
        {
            var at = now > j.ScheduledDepartureUtc ? now : j.ScheduledDepartureUtc;
            var va = j.VehicleAssignments.FirstOrDefault(a => a.Covers(at)) ?? j.VehicleAssignments.OrderByDescending(a => a.ValidFrom).FirstOrDefault();
            var da = j.DriverAssignments.FirstOrDefault(a => a.Covers(at)) ?? j.DriverAssignments.OrderByDescending(a => a.ValidFrom).FirstOrDefault();

            var stops = j.Stops.OrderBy(s => s.Sequence).Select(s => new OpsStopData(
                s.Sequence, s.StationCode, s.Kind, s.State, s.ScheduledArrivalUtc, s.ScheduledDepartureUtc, s.EstimatedArrivalUtc, s.EstimatedDepartureUtc,
                s.ActualArrivalUtc, s.ActualDepartureUtc, s.PlatformCode, s.PlatformOriginalCode, s.PlatformState, s.EstimateSource)).ToList();

            return new OpsJourneyData(
                j.Id, j.ServiceDate, j.TrainNumber, j.RouteCode, j.OriginStationCode, j.DestinationStationCode, j.ScheduledDepartureUtc, j.ScheduledArrivalUtc,
                j.Lifecycle, j.Confidence, j.DelayMinutes, j.PunctualityBand, j.LastAcceptedUpdateAt, j.CurrentStopSequence, j.Version,
                j.CancelReasonCode, j.EndReasonCode, j.ControllerNote, j.EndedAt, j.EndedAtStopSequence,
                va?.VehicleId, va is null ? null : vehicles.GetValueOrDefault(va.VehicleId), da?.DutyId, da is null ? null : duties.GetValueOrDefault(da.DutyId), stops);
        }).ToList();
    }
}

internal sealed class PlanningStore(JourneysDbContext context) : IPlanningStore
{
    public Task<Vehicle?> GetVehicleAsync(Guid id, CancellationToken cancellationToken) =>
        context.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<CrewDuty?> GetDutyAsync(Guid id, CancellationToken cancellationToken) =>
        context.CrewDuties.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public void AddDuty(CrewDuty duty) => context.CrewDuties.Add(duty);

    public void AddUnavailability(VehicleUnavailability period) => context.VehicleUnavailabilities.Add(period);

    public Task<VehicleUnavailability?> GetUnavailabilityAsync(Guid id, CancellationToken cancellationToken) =>
        context.VehicleUnavailabilities.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
}

internal sealed class TelemetryHold(JourneysDbContext context) : ITelemetryHold
{
    public Task PlaceAsync(Guid journeyId, CancellationToken cancellationToken) =>
        context.TelemetryReadings.Where(t => t.ResolvedJourneyId == journeyId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.LegalHold, true), cancellationToken);
}
