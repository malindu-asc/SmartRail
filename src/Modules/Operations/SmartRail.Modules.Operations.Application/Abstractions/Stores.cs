using SmartRail.Application.Core.Abstractions;
using SmartRail.Modules.Operations.Domain.Incidents;
using SmartRail.Modules.Operations.Domain.Messages;

namespace SmartRail.Modules.Operations.Application.Abstractions;

// This module's audit log: disruptions, incidents and driver messages (the operational audit view).
public interface IOperationsAuditLog : IAuditLog;

public interface IIncidentStore
{
    // With its timeline, tracked for update.
    Task<Incident?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Incident>> ListAsync(bool openOnly, CancellationToken cancellationToken);

    Task<int> CountOpenOwnedByAsync(string ownerSubject, CancellationToken cancellationToken);

    void Add(Incident incident);
}

public interface IMessageStore
{
    Task<ControlMessage?> GetAsync(Guid id, CancellationToken cancellationToken);

    // Today's messages (sent since the instant), newest first.
    Task<IReadOnlyList<ControlMessage>> ListSinceAsync(DateTimeOffset since, CancellationToken cancellationToken);

    // A driver's own messages, still valid or sent recently.
    Task<IReadOnlyList<ControlMessage>> ListForDriverAsync(string driverSubject, DateTimeOffset since, CancellationToken cancellationToken);

    Task<IReadOnlyList<ControlMessage>> ListForIncidentAsync(Guid incidentId, CancellationToken cancellationToken);

    void Add(ControlMessage message);
}

public interface IAssistanceStore
{
    Task<AssistanceRequest?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AssistanceRequest>> ListOpenAsync(CancellationToken cancellationToken);

    void Add(AssistanceRequest request);
}
