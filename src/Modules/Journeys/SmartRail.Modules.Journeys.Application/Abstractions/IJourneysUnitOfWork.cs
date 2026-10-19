using SmartRail.SharedKernel.Abstractions;

namespace SmartRail.Modules.Journeys.Application.Abstractions;

public interface IJourneysUnitOfWork : IUnitOfWork
{
    // Saves the pending changes. Returns false, with every pending change discarded, when another writer got there first
    // (the journey was changed meanwhile, or the same message was stored by a parallel request). The caller starts again.
    Task<bool> TrySaveAsync(CancellationToken cancellationToken);
}
