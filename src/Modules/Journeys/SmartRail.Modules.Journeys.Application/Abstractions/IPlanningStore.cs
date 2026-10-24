using SmartRail.Modules.Journeys.Domain.Crew;
using SmartRail.Modules.Journeys.Domain.Fleet;

namespace SmartRail.Modules.Journeys.Application.Abstractions;

// Write side for planning: units, duties and out-of-service periods. Journeys themselves go through IJourneyRepository.
public interface IPlanningStore
{
    Task<Vehicle?> GetVehicleAsync(Guid id, CancellationToken cancellationToken);

    Task<CrewDuty?> GetDutyAsync(Guid id, CancellationToken cancellationToken);

    void AddDuty(CrewDuty duty);

    void AddUnavailability(VehicleUnavailability period);

    Task<VehicleUnavailability?> GetUnavailabilityAsync(Guid id, CancellationToken cancellationToken);
}

// Keeps a journey's raw telemetry as evidence (REQ-OPS-008): the retention purge never removes a reading under legal hold.
public interface ITelemetryHold
{
    Task PlaceAsync(Guid journeyId, CancellationToken cancellationToken);
}
