using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.Core.Events;
using SmartRail.SharedKernel.Events;

namespace SmartRail.Infrastructure.Core.EventBus;

// No broker: integration events are delivered to handlers in the same process.
// Delivery is at-least-once because this runs from the outbox processor.
public sealed class InProcessEventBus(IServiceProvider serviceProvider) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        foreach (var handler in serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>())
            await handler.Handle(integrationEvent, cancellationToken);
    }
}
