namespace SmartRail.Application.Core.Abstractions;

// One high-consequence action: who did what to which thing, why, and what changed (REQ-OPS-011).
public sealed record AuditRecord(
    string ActorSubject,
    string? ActorRole,
    string Action,
    string TargetType,
    string? TargetId,
    string? Reason,
    object? Before = null,
    object? After = null,
    string? CorrelationId = null);

// Writes an audit row in the same unit of work as the change, so both commit together or not at all.
// Each module has its own table and its own implementation (a marker interface per module extends this one).
public interface IAuditLog
{
    void Record(AuditRecord record);
}
