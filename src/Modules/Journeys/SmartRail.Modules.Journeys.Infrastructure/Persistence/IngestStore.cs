using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Live;

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence;

internal sealed class IngestStore(JourneysDbContext context) : IIngestStore
{
    public async Task<bool> SequenceSeenAsync(Guid sourceId, long sequence, CancellationToken cancellationToken) =>
        await context.JourneyEvents.AsNoTracking().AnyAsync(e => e.SourceId == sourceId && e.DeviceSequence == sequence, cancellationToken)
        || await context.TelemetryReadings.AsNoTracking().AnyAsync(r => r.SourceId == sourceId && r.Sequence == sequence, cancellationToken);

    public async Task<long?> HighestAcceptedSequenceAsync(Guid sourceId, CancellationToken cancellationToken)
    {
        var events = await context.JourneyEvents
            .AsNoTracking()
            .Where(e => e.SourceId == sourceId
                        && (e.Disposition == EventDisposition.Applied
                            || e.Disposition == EventDisposition.Superseded
                            || e.Disposition == EventDisposition.HeldForCorrection))
            .MaxAsync(e => (long?)e.DeviceSequence, cancellationToken);

        var readings = await context.TelemetryReadings
            .AsNoTracking()
            .Where(r => r.SourceId == sourceId && r.Accepted)
            .MaxAsync(r => (long?)r.Sequence, cancellationToken);

        return events is null ? readings : readings is null ? events : Math.Max(events.Value, readings.Value);
    }

    public Task<JourneyEvent?> FindWinningEventAsync(Guid journeyId, int stopSequence, StopEventType type, CancellationToken cancellationToken) =>
        context.JourneyEvents
            .Where(e => e.JourneyId == journeyId
                        && e.StopSequence == stopSequence
                        && e.EventType == type
                        && e.Disposition == EventDisposition.Applied)
            .OrderBy(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid?> ResolveJourneyAsync(string vehicleCode, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var journeyId = await (
                from vehicle in context.Vehicles.AsNoTracking()
                join assignment in context.Set<SmartRail.Modules.Journeys.Domain.Fleet.VehicleAssignment>().AsNoTracking()
                    on vehicle.Id equals assignment.VehicleId
                where vehicle.Code == vehicleCode
                      && assignment.ValidFrom <= at
                      && (assignment.ValidTo == null || assignment.ValidTo > at)
                select (Guid?)assignment.JourneyId)
            .FirstOrDefaultAsync(cancellationToken);

        return journeyId;
    }

    public Task<TelemetryReading?> LastAcceptedReadingAsync(string vehicleCode, Guid journeyId, CancellationToken cancellationToken) =>
        context.TelemetryReadings
            .AsNoTracking()
            .Where(r => r.VehicleCode == vehicleCode && r.ResolvedJourneyId == journeyId && r.Accepted)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(JourneyEvent journeyEvent) => context.JourneyEvents.Add(journeyEvent);

    public void Add(TelemetryReading reading) => context.TelemetryReadings.Add(reading);
}
