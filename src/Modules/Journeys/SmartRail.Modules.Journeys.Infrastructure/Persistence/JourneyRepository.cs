using Microsoft.EntityFrameworkCore;
using SmartRail.Infrastructure.Core.Persistence;
using SmartRail.Modules.Journeys.Domain.Journeys;

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence;

internal sealed class JourneyRepository(JourneysDbContext context)
    : BaseRepository<Journey, Guid, JourneysDbContext>(context), IJourneyRepository
{
    public async Task<IReadOnlySet<Guid>> GetGeneratedServiceIdsAsync(DateOnly serviceDate, CancellationToken cancellationToken = default)
    {
        var ids = await Context.Journeys
            .AsNoTracking()
            .Where(j => j.ServiceDate == serviceDate && j.ServiceId != null)
            .Select(j => j.ServiceId!.Value)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }
}
