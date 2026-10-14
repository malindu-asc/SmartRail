using MediatR;
using SmartRail.SharedKernel.Events;
using SmartRail.SharedKernel.Primitives;

namespace SmartRail.Application.Core.Events;

// Wraps a domain event so the Domain layer never depends on MediatR.
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;

public interface IDomainEventHandler<TDomainEvent> : INotificationHandler<DomainEventNotification<TDomainEvent>>
    where TDomainEvent : IDomainEvent;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}

// Lives in the reacting module's Application layer. Must be idempotent:
// an outbox retry runs every handler for that message again.
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task Handle(TEvent integrationEvent, CancellationToken cancellationToken);
}
