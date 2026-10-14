using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartRail.Infrastructure.Core.Persistence;
using SmartRail.SharedKernel.Primitives;

namespace SmartRail.Infrastructure.Core.Outbox;

// Runs inside the same transaction as the entity changes: domain events become outbox rows,
// so a state change and its events are saved together or not at all.
public sealed class OutboxMessagesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is BaseDbContext context)
            ConvertDomainEventsToOutboxMessages(context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ConvertDomainEventsToOutboxMessages(BaseDbContext context)
    {
        var entities = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var messages = entities
            .SelectMany(e => e.DomainEvents)
            .Select(domainEvent => new OutboxMessage
            {
                Type = domainEvent.GetType().AssemblyQualifiedName!,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                OccurredOnUtc = domainEvent.OccurredOnUtc
            })
            .ToList();

        context.Set<OutboxMessage>().AddRange(messages);
        entities.ForEach(e => e.ClearDomainEvents());
    }
}
