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

// Draft: saved, not sent. AwaitingApproval: a network-wide notice waits for a second person (REQ-OPS-005).
// Published: passengers see it. Rejected: the second person sent it back with a comment.
public enum DisruptionStatus
{
    Draft = 0,
    Published = 1,
    Resolved = 2,
    Retracted = 3,
    AwaitingApproval = 4,
    Rejected = 5
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

    // What kind of trouble (for example "Infrastructure fault", "Planned works"); free text chosen from the console's list.
    public string? Type { get; private set; }

    // Planned works start later: passengers can see them in advance (REQ-OPS-016). Null means "now".
    public DateTimeOffset? StartsAtUtc { get; private set; }

    // Every disruption has a review-by time; past it, the disruption is "expired - review" (REQ-OPS-006).
    public DateTimeOffset ReviewByUtc { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public string? RejectedBy { get; private set; }
    public string? RejectionComment { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }

    // submit false saves a draft. submit true publishes it, or sends a network-wide notice to a second approver.
    public static Result<Disruption> Create(
        string title, string message, DisruptionSeverity severity, bool networkWide, IEnumerable<string> stationCodes,
        DateTimeOffset? expectedEnd, DateTimeOffset reviewBy, string createdBy, DateTimeOffset now, Guid? id = null,
        string? type = null, DateTimeOffset? startsAt = null, bool submit = true)
    {
        var codes = Normalise(stationCodes);

        var valid = Validate(title, message, networkWide, codes, expectedEnd, startsAt, reviewBy, now);
        if (valid.IsFailure)
            return valid.Error;

        var disruption = new Disruption(id ?? Guid.NewGuid())
        {
            Title = title.Trim(),
            Message = message.Trim(),
            Severity = severity,
            NetworkWide = networkWide,
            StationCodes = networkWide ? [] : codes,
            ExpectedEndUtc = expectedEnd,
            StartsAtUtc = startsAt,
            Type = string.IsNullOrWhiteSpace(type) ? null : type.Trim(),
            ReviewByUtc = reviewBy,
            CreatedBy = createdBy,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Status = DisruptionStatus.Draft
        };

        if (submit)
            disruption.Submit(now);

        return disruption;
    }

    // Sends a draft (or a rejected notice) on its way. A network-wide notice is high harm if wrong, so it waits for a second person.
    public Result Submit(DateTimeOffset now)
    {
        if (Status is not (DisruptionStatus.Draft or DisruptionStatus.Rejected))
            return OperationsErrors.NotDraft;
        if (ReviewByUtc <= now)
            return OperationsErrors.ReviewByRequired;

        SubmittedAtUtc = now;
        UpdatedAtUtc = now;
        RejectedBy = null;
        RejectionComment = null;

        if (NetworkWide)
        {
            Status = DisruptionStatus.AwaitingApproval;
        }
        else
        {
            Status = DisruptionStatus.Published;
            PublishedAtUtc = now;
        }

        return Result.Success();
    }

    public Result Approve(string approver, DateTimeOffset now)
    {
        if (Status != DisruptionStatus.AwaitingApproval)
            return OperationsErrors.NotAwaitingApproval;
        if (string.Equals(approver, CreatedBy, StringComparison.Ordinal))
            return OperationsErrors.SecondApproverRequired;

        Status = DisruptionStatus.Published;
        ApprovedBy = approver;
        PublishedAtUtc = now;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    // The second person says no. A comment is required, and the author can fix it and send it again.
    public Result Reject(string approver, string comment, DateTimeOffset now)
    {
        if (Status != DisruptionStatus.AwaitingApproval)
            return OperationsErrors.NotAwaitingApproval;
        if (string.Equals(approver, CreatedBy, StringComparison.Ordinal))
            return OperationsErrors.SecondApproverRequired;
        if (string.IsNullOrWhiteSpace(comment))
            return OperationsErrors.CommentRequired;

        Status = DisruptionStatus.Rejected;
        RejectedBy = approver;
        RejectionComment = comment.Trim()[..Math.Min(comment.Trim().Length, 500)];
        UpdatedAtUtc = now;
        return Result.Success();
    }

    // Edit while it is a draft, was rejected, or is live and local. A live network-wide notice cannot be edited (retract it and write again).
    public Result Update(
        string title, string message, DisruptionSeverity severity, DateTimeOffset? expectedEnd, DateTimeOffset reviewBy, DateTimeOffset now,
        IEnumerable<string>? stationCodes = null, string? type = null, DateTimeOffset? startsAt = null)
    {
        if (Status is DisruptionStatus.Resolved or DisruptionStatus.Retracted)
            return OperationsErrors.AlreadyClosed;
        if (Status == DisruptionStatus.AwaitingApproval || (Status == DisruptionStatus.Published && NetworkWide))
            return OperationsErrors.NotEditable;

        var codes = stationCodes is null ? StationCodes : Normalise(stationCodes);
        var valid = Validate(title, message, NetworkWide, codes, expectedEnd, startsAt ?? StartsAtUtc, reviewBy, now);
        if (valid.IsFailure)
            return valid;

        Title = title.Trim();
        Message = message.Trim();
        Severity = severity;
        ExpectedEndUtc = expectedEnd;
        ReviewByUtc = reviewBy;
        StationCodes = NetworkWide ? [] : codes.ToList();
        if (type is not null)
            Type = string.IsNullOrWhiteSpace(type) ? null : type.Trim();
        if (startsAt is not null)
            StartsAtUtc = startsAt;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    // It is over.
    public Result Resolve(DateTimeOffset now) => Close(DisruptionStatus.Resolved, now);

    // It was wrong: it disappears from every client (REQ-PAX-009).
    public Result Retract(DateTimeOffset now) => Close(DisruptionStatus.Retracted, now);

    public bool IsExpired(DateTimeOffset now) => Status == DisruptionStatus.Published && ReviewByUtc <= now;

    // Planned works that have not started yet.
    public bool IsPlanned(DateTimeOffset now) => Status == DisruptionStatus.Published && StartsAtUtc is { } start && start > now;

    private Result Close(DisruptionStatus status, DateTimeOffset now)
    {
        if (Status is DisruptionStatus.Resolved or DisruptionStatus.Retracted)
            return OperationsErrors.AlreadyClosed;

        Status = status;
        UpdatedAtUtc = now;
        return Result.Success();
    }

    private static List<string> Normalise(IEnumerable<string> stationCodes) =>
        stationCodes.Select(c => c.Trim().ToUpperInvariant()).Where(c => c.Length > 0).Distinct().ToList();

    private static Result Validate(
        string title, string message, bool networkWide, IReadOnlyList<string> codes, DateTimeOffset? expectedEnd,
        DateTimeOffset? startsAt, DateTimeOffset reviewBy, DateTimeOffset now)
    {
        if (!ValidText(title, message))
            return OperationsErrors.TextInvalid;
        if (!networkWide && codes.Count == 0)
            return OperationsErrors.ScopeRequired;
        if (reviewBy <= now)
            return OperationsErrors.ReviewByRequired;
        if (startsAt is { } start && expectedEnd is { } end && end <= start)
            return OperationsErrors.PeriodInvalid;

        return Result.Success();
    }

    private static bool ValidText(string title, string message) =>
        !string.IsNullOrWhiteSpace(title) && title.Trim().Length <= 120
        && !string.IsNullOrWhiteSpace(message) && message.Trim().Length <= 1000;
}
