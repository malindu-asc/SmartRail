using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRail.Application.Core.Events;
using SmartRail.Infrastructure.Core.Persistence;
using SmartRail.SharedKernel.Primitives;

namespace SmartRail.Infrastructure.Core.Outbox;

public interface IOutboxMessageProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken = default);
}

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public bool Enabled { get; init; } = true;
    public int IntervalMilliseconds { get; init; } = 1000;
    public int BatchSize { get; init; } = 20;
    public int MaxAttempts { get; init; } = 5;
}

// One registration per module DbContext. Dispatches domain events in-process via MediatR.
public sealed class OutboxMessageProcessor<TContext>(
    TContext context,
    IPublisher publisher,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxMessageProcessor<TContext>> logger) : IOutboxMessageProcessor
    where TContext : BaseDbContext
{
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var messages = await context.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null && m.DeadLetteredOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(settings.BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.Type)
                    ?? throw new InvalidOperationException($"Type '{message.Type}' could not be resolved.");
                var domainEvent = (IDomainEvent?)JsonSerializer.Deserialize(message.Content, eventType)
                    ?? throw new InvalidOperationException("Deserialisation returned null.");

                var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
                var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

                await publisher.Publish(notification, cancellationToken);

                message.ProcessedOnUtc = timeProvider.GetUtcNow();
                message.Error = null;
                SmartRailTelemetry.OutboxProcessed.Add(1, new KeyValuePair<string, object?>("module", typeof(TContext).Name));
            }
            catch (Exception ex)
            {
                // Never set ProcessedOnUtc here: the message must be retried on the next poll.
                message.Attempts++;
                message.Error = ex.ToString();
                SmartRailTelemetry.OutboxFailed.Add(1, new KeyValuePair<string, object?>("module", typeof(TContext).Name));

                if (message.Attempts >= settings.MaxAttempts)
                {
                    message.DeadLetteredOnUtc = timeProvider.GetUtcNow();
                    SmartRailTelemetry.OutboxDeadLettered.Add(1, new KeyValuePair<string, object?>("module", typeof(TContext).Name));
                    logger.LogError(ex, "Outbox message {Id} dead-lettered after {Attempts} attempts", message.Id, message.Attempts);
                }
                else
                {
                    logger.LogWarning(ex, "Outbox message {Id} failed, attempt {Attempts}", message.Id, message.Attempts);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
