namespace SmartRail.Application.Core.Abstractions;

// Reads one module's audit_entries table (REQ-OPS-011). The audit log page merges every source the caller may see.
public interface IAuditSource
{
    // "identity", "journeys", "operations", "network"
    string Module { get; }

    // True for the sources that hold access changes (users, roles, devices, security); false for operational ones.
    bool IsAccess { get; }

    Task<IReadOnlyList<AuditRow>> QueryAsync(AuditFilter filter, CancellationToken cancellationToken);
}

public sealed record AuditFilter(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Actor,
    string? Action,
    string? Text,
    int Take);

public sealed record AuditRow(
    long Id,
    string Module,
    DateTimeOffset OccurredAt,
    string ActorSubject,
    string? ActorRole,
    string Action,
    string TargetType,
    string? TargetId,
    string? Reason,
    string? Before,
    string? After);
