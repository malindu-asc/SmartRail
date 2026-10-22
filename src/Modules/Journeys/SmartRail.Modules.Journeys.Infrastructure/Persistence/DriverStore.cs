using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Crew;

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence;

internal sealed class DriverStore(JourneysDbContext context, IPlanApprovals approvals, IConfiguration configuration) : IDriverStore
{
    public Task<CrewDuty?> GetDutyAsync(Guid dutyId, CancellationToken cancellationToken) =>
        context.CrewDuties.FirstOrDefaultAsync(d => d.Id == dutyId, cancellationToken);

    public async Task<IReadOnlyList<DutyData>> ListDutiesAsync(string driverSubject, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var duties = await context.CrewDuties.AsNoTracking()
            .Where(d => d.DriverSubject == driverSubject && d.ServiceDate >= from && d.ServiceDate <= to)
            .OrderBy(d => d.RosteredFrom).ToListAsync(cancellationToken);

        // A day's duties only reach the driver once the scheduler has approved that day's plan (Journeys:Planning:ApprovalRequired).
        if (configuration.GetValue("Journeys:Planning:ApprovalRequired", true))
        {
            var approved = await approvals.ApprovedAsync(from, to, cancellationToken);
            duties = duties.Where(d => approved.Contains(d.ServiceDate)).ToList();
        }

        var ids = duties.Select(d => d.Id).ToList();
        var journeys = await context.Journeys.AsNoTracking().AsSplitQuery()
            .Include(j => j.DriverAssignments).Include(j => j.VehicleAssignments).Include(j => j.Stops)
            .Where(j => j.DriverAssignments.Any(a => ids.Contains(a.DutyId)))
            .ToListAsync(cancellationToken);

        var vehicleIds = journeys.SelectMany(j => j.VehicleAssignments).Select(a => a.VehicleId).Distinct().ToList();
        var codes = await context.Vehicles.AsNoTracking().Where(v => vehicleIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v.Code, cancellationToken);

        return duties.Select(duty =>
        {
            var mine = journeys.Where(j => j.DriverAssignments.Any(a => a.DutyId == duty.Id)).OrderBy(j => j.ScheduledDepartureUtc).ToList();
            var unit = mine.SelectMany(j => j.VehicleAssignments).OrderBy(a => a.ValidFrom).Select(a => codes.GetValueOrDefault(a.VehicleId)).FirstOrDefault(c => c is not null);

            return new DutyData(
                duty.Id, duty.ServiceDate, duty.RosteredFrom, duty.RosteredTo, duty.State, duty.ReviewState, duty.SignedOnAt, duty.SignedOffAt, unit,
                mine.Select(j => new DutyJourneyData(
                    j.Id, j.TrainNumber, j.OriginStationCode, j.DestinationStationCode, j.ScheduledDepartureUtc, j.ScheduledArrivalUtc, j.Lifecycle,
                    j.DelayMinutes, j.Stops.Count, j.Stops.OrderBy(s => s.Sequence).FirstOrDefault()?.PlatformCode)).ToList());
        }).ToList();
    }

    public async Task<DriverJourneyData?> GetJourneyAsync(Guid journeyId, string driverSubject, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var journey = await context.Journeys.AsNoTracking().AsSplitQuery()
            .Include(j => j.DriverAssignments).Include(j => j.VehicleAssignments).Include(j => j.Stops)
            .FirstOrDefaultAsync(j => j.Id == journeyId, cancellationToken);
        if (journey is null)
            return null;

        var dutyIds = journey.DriverAssignments.Select(a => a.DutyId).ToList();
        var duty = await context.CrewDuties.AsNoTracking().Where(d => dutyIds.Contains(d.Id) && d.DriverSubject == driverSubject)
            .OrderByDescending(d => d.RosteredFrom).FirstOrDefaultAsync(cancellationToken);
        if (duty is null)
            return null;

        var vehicleId = journey.VehicleAssignments.OrderBy(a => a.ValidFrom).Select(a => (Guid?)a.VehicleId).FirstOrDefault();
        var unit = vehicleId is null ? null : await context.Vehicles.AsNoTracking().Where(v => v.Id == vehicleId).Select(v => v.Code).FirstOrDefaultAsync(cancellationToken);

        return new DriverJourneyData(
            journey.Id, journey.TrainNumber, journey.OriginStationCode, journey.DestinationStationCode, journey.Lifecycle, journey.DelayMinutes,
            journey.DelayReasonCode, duty.Id, duty.State, unit,
            journey.Stops.OrderBy(s => s.Sequence).Select(s => new DriverStopData(
                s.Sequence, s.StationCode, s.Kind, s.State, s.ScheduledArrivalUtc, s.ScheduledDepartureUtc, s.EstimatedArrivalUtc,
                s.EstimatedDepartureUtc, s.ActualArrivalUtc, s.ActualDepartureUtc, s.PlatformCode, s.PlatformOriginalCode)).ToList());
    }
}
