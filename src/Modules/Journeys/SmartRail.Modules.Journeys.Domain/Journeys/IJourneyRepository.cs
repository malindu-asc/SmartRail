using SmartRail.SharedKernel.Abstractions;

namespace SmartRail.Modules.Journeys.Domain.Journeys;

public interface IJourneyRepository : IRepository<Journey, Guid>
{
    // The journey with its stops, tracked for update (live ingest changes the stops with the journey).
    Task<Journey?> GetWithStopsAsync(Guid journeyId, CancellationToken cancellationToken = default);

    // The services that already have a generated journey on a service date (generation never creates a second one).
    Task<IReadOnlySet<Guid>> GetGeneratedServiceIdsAsync(DateOnly serviceDate, CancellationToken cancellationToken = default);
}
