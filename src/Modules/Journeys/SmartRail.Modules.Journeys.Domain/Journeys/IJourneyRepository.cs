using SmartRail.SharedKernel.Abstractions;

namespace SmartRail.Modules.Journeys.Domain.Journeys;

public interface IJourneyRepository : IRepository<Journey, Guid>
{
    // The services that already have a generated journey on a service date (generation never creates a second one).
    Task<IReadOnlySet<Guid>> GetGeneratedServiceIdsAsync(DateOnly serviceDate, CancellationToken cancellationToken = default);
}
