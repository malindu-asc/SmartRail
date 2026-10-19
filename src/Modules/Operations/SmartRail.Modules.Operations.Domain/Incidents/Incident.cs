using SmartRail.Modules.Operations.Domain.Errors;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Operations.Domain.Incidents;

public enum IncidentSeverity
{
    Minor = 0,
    Major = 1,
    Critical = 2
}

public enum IncidentStatus
{
    Open = 0,
    Resolved = 1
}

// Note: a person wrote it. Action: something was done (a driver told to hold). Handover: the owner changed.
// System: the platform recorded it (opened, disruption published, journey terminated, resolved).
public enum IncidentEntryKind
{
    Note = 0,
    Action = 1,
    Handover = 2,
    System = 3
}

// A serious event with an owner and a timeline (REQ-OPS-007). Only the Incident Manager capability (the Network Controller)
// can declare one, and only that capability can declare a Critical one, behind a confirmation and a reason.
public sealed class Incident : Entity<Guid>
{
    private readonly List<IncidentEntry> _entries = [];

    private Incident() { }

    private Incident(Guid id) : base(id) { }

    public string Title { get; private set; } = string.Empty;
    public IncidentSeverity Severity { get; private set; }
    public IncidentStatus Status { get; private set; }
    public string OwnerSubject { get; private set; } = string.Empty;
    public string OpenedBy { get; private set; } = string.Empty;
    public DateTimeOffset OpenedAtUtc { get; private set; }

    // "Reported by signaller", "Reported by driver Michael Fernando".
    public string? ReportedBy { get; private set; }
    public List<string> StationCodes { get; private set; } = [];
    public Guid? DisruptionId { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public string? ResolvedBy { get; private set; }
    public string? ResolutionNote { get; private set; }
    public IReadOnlyList<IncidentEntry> Entries => _entries;

    public static Result<Incident> Declare(
        string title, IncidentSeverity severity, string openedBy, string? reportedBy, IEnumerable<string> stationCodes, Guid? disruptionId,
        DateTimeOffset now, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 120)
            return OperationsErrors.IncidentTitleInvalid;

        var incident = new Incident(id ?? Guid.NewGuid())
        {
            Title = title.Trim(),
            Severity = severity,
            Status = IncidentStatus.Open,
            OwnerSubject = openedBy,
            OpenedBy = openedBy,
            OpenedAtUtc = now,
            ReportedBy = string.IsNullOrWhiteSpace(reportedBy) ? null : reportedBy.Trim(),
            StationCodes = stationCodes.Select(c => c.Trim().ToUpperInvariant()).Where(c => c.Length > 0).Distinct().ToList(),
            DisruptionId = disruptionId
        };

        incident.Add(IncidentEntryKind.System, "Incident opened", incident.ReportedBy, openedBy, now);
        return incident;
    }

    public Result AddNote(IncidentEntryKind kind, string text, string? detail, string author, DateTimeOffset now)
    {
        if (Status == IncidentStatus.Resolved)
            return OperationsErrors.IncidentClosed;
        if (string.IsNullOrWhiteSpace(text))
            return OperationsErrors.NoteRequired;

        Add(kind, text.Trim(), detail, author, now);
        return Result.Success();
    }

    // Ownership moves with a note, so the incident is never left without a person responsible.
    public Result HandOver(string newOwner, string note, string by, DateTimeOffset now)
    {
        if (Status == IncidentStatus.Resolved)
            return OperationsErrors.IncidentClosed;
        if (string.IsNullOrWhiteSpace(note))
            return OperationsErrors.NoteRequired;
        if (string.Equals(newOwner, OwnerSubject, StringComparison.Ordinal))
            return OperationsErrors.SameOwner;

        OwnerSubject = newOwner;
        Add(IncidentEntryKind.Handover, "Handed over", note.Trim(), by, now);
        return Result.Success();
    }

    public Result LinkDisruption(Guid disruptionId, string title, string by, DateTimeOffset now)
    {
        if (Status == IncidentStatus.Resolved)
            return OperationsErrors.IncidentClosed;

        DisruptionId = disruptionId;
        Add(IncidentEntryKind.System, $"Disruption linked: {title}", null, by, now);
        return Result.Success();
    }

    public Result Resolve(string note, string by, DateTimeOffset now)
    {
        if (Status == IncidentStatus.Resolved)
            return OperationsErrors.IncidentClosed;
        if (string.IsNullOrWhiteSpace(note))
            return OperationsErrors.NoteRequired;

        Status = IncidentStatus.Resolved;
        ResolvedAtUtc = now;
        ResolvedBy = by;
        ResolutionNote = note.Trim();
        Add(IncidentEntryKind.System, "Incident resolved", note.Trim(), by, now);
        return Result.Success();
    }

    // Serious enough to raise to Critical, for example when a train has to be terminated in an emergency.
    public void RaiseToCritical(string by, DateTimeOffset now)
    {
        if (Severity == IncidentSeverity.Critical)
            return;

        Severity = IncidentSeverity.Critical;
        Add(IncidentEntryKind.System, "Raised to Critical", null, by, now);
    }

    private void Add(IncidentEntryKind kind, string text, string? detail, string author, DateTimeOffset now) =>
        _entries.Add(new IncidentEntry(Id, kind, text, detail, author, now));
}

public sealed class IncidentEntry
{
    private IncidentEntry() { }

    internal IncidentEntry(Guid incidentId, IncidentEntryKind kind, string text, string? detail, string author, DateTimeOffset at)
    {
        Id = Guid.NewGuid();
        IncidentId = incidentId;
        Kind = kind;
        Text = text.Length <= 300 ? text : text[..300];
        Detail = detail is null ? null : (detail.Length <= 300 ? detail : detail[..300]);
        Author = author;
        At = at;
    }

    public Guid Id { get; private set; }
    public Guid IncidentId { get; private set; }
    public IncidentEntryKind Kind { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public string? Detail { get; private set; }
    public string Author { get; private set; } = string.Empty;
    public DateTimeOffset At { get; private set; }
}
