using SmartRail.Modules.Operations.Domain.Errors;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Operations.Domain.Disruptions;

public enum DisruptionSeverity
{
    Minor = 0,
    Major = 1,
    Severe = 2
}

public enum DisruptionStatus
{
    // Network-wide notices wait here for a second person to approve (REQ-OPS-005).
    Draft = 0,
    Published = 1,
    Resolved = 2,
    Retracted = 3
}

// A notice to passengers about trouble on the network: what, where, how bad, and when it is expected to clear.
// The text is plain text; clients never render it as markup (REQ-OPS-005 content rules).
public sealed class Disruption : Entity<Guid>
{
    private Disruption() { }

    private Disruption(Guid id) : base(id) { }

    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public DisruptionSeverity Severity { get; private set; }
    public DisruptionStatus Status { get; private set; }
    public bool NetworkWide { get; private set; }
    public List<string> StationCodes { get; private set; } = [];
    public DateTimeOffset? ExpectedEndUtc { get; private set; }

    // Every disruption has a review-by time; past it, the disruption is "expired - review" (REQ-OPS-006).
    public DateTimeOffset ReviewByUtc { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public static Result<Disruption> Create(
        string title, string message, DisruptionSeverity severity, bool networkWide, IEnumerable<string> stationCodes,
        DateTimeOffset? expectedEnd, DateTimeOffset reviewBy, string createdBy, DateTimeOffset now, Guid? id = null)
    {
        var codes = stationCodes.Select(c => c.Trim().ToUpperInvariant()).Where(c => c.Length > 0).Distinct().ToList();

        if (!ValidText(title, message))
            return OperationsErrors.TextInvalid;
        if (!networkWide && codes.Count == 0)
            return OperationsErrors.ScopeRequired;
        if (reviewBy <= now)
            return OperationsErrors.ReviewByRequired;

        var disruption = new Disruption(id ?? Guid.NewGuid())
        {
            Title = title.Trim(),
            Message = message.Trim(),
            Severity = severity,
            NetworkWide = networkWide,
            StationCodes = networkWide ? [] : codes,
            ExpectedEndUtc = expectedEnd,
            ReviewByUtc = reviewBy,
            CreatedBy = createdBy,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            // A network-wide notice is high harm if wrong, so it waits for a second person.
            Status = networkWide ? DisruptionStatus.Draft : DisruptionStatus.Published,
            PublishedAtUtc = networkWide ? null : now
        };

        return disruption;
    }

    public Result Approve(string approver, DateTimeOffset now)
    {
        if (Status != DisruptionStatus.Draft)
            return OperationsErrors.NotDraft;
        if (string.Equals(approver, CreatedBy, StringComparison.Ordinal))
            return OperationsErrors.SecondApproverRequired;

        Status = DisruptionStatus.Published;
        ApprovedBy = approver;
        PublishedAtUtc = now;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    public Result Update(string title, string message, DisruptionSeverity severity, DateTimeOffset? expectedEnd, DateTimeOffset reviewBy, DateTimeOffset now)
    {
        if (Status is DisruptionStatus.Resolved or DisruptionStatus.Retracted)
            return OperationsErrors.AlreadyClosed;
        if (!ValidText(title, message))
            return OperationsErrors.TextInvalid;
        if (reviewBy <= now)
            return OperationsErrors.ReviewByRequired;

        Title = title.Trim();
        Message = message.Trim();
        Severity = severity;
        ExpectedEndUtc = expectedEnd;
        ReviewByUtc = reviewBy;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    // It is over.
    public Result Resolve(DateTimeOffset now) => Close(DisruptionStatus.Resolved, now);

    // It was wrong: it disappears from every client (REQ-PAX-009).
    public Result Retract(DateTimeOffset now) => Close(DisruptionStatus.Retracted, now);

    public bool IsExpired(DateTimeOffset now) => Status == DisruptionStatus.Published && ReviewByUtc <= now;

    private Result Close(DisruptionStatus status, DateTimeOffset now)
    {
        if (Status is DisruptionStatus.Resolved or DisruptionStatus.Retracted)
            return OperationsErrors.AlreadyClosed;

        Status = status;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    private static bool ValidText(string title, string message) =>
        !string.IsNullOrWhiteSpace(title) && title.Trim().Length <= 120
        && !string.IsNullOrWhiteSpace(message) && message.Trim().Length <= 1000;
}
