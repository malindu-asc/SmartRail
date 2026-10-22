using SmartRail.Application.Core.Abstractions;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Journeys.Contracts;
using SmartRail.Modules.Network.Contracts;
using SmartRail.Modules.Operations.Application.Abstractions;
using SmartRail.Modules.Operations.Application.Disruptions;
using SmartRail.Modules.Operations.Domain.Disruptions;
using SmartRail.Modules.Operations.Domain.Errors;
using SmartRail.Modules.Operations.Domain.Incidents;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Operations.Application.Incidents;

// ---- views (the Incident screen) ----

public sealed record IncidentRow(
    Guid Id,
    string Title,
    string Severity,
    string Status,
    string OwnerSubject,
    string? OwnerName,
    DateTimeOffset OpenedAtUtc,
    IReadOnlyList<string> StationCodes);

public sealed record IncidentEntryView(DateTimeOffset At, string Kind, string Text, string? Detail, string Author, string? AuthorName);

// Who has been told. PassengerAlerts is null until passengers can follow journeys on the server (SPEC-020).
public sealed record IncidentTold(int? PassengerAlerts, int StationDisplays, int DriverMessages, int DriverMessagesAcknowledged);

public sealed record IncidentLinked(Guid? DisruptionId, string? DisruptionTitle, DateTimeOffset? ReviewByUtc, int JourneysAffected);

public sealed record IncidentDetail(
    IncidentRow Incident,
    string? ReportedBy,
    DateTimeOffset? ResolvedAtUtc,
    string? ResolutionNote,
    IReadOnlyList<IncidentEntryView> Timeline,
    IncidentTold Told,
    IncidentLinked Linked);

// What a signed-in controller still owns when they sign out (the sign-out dialog): an incident must be handed over first.
public sealed record SignOutCheck(IReadOnlyList<IncidentRow> OwnedIncidents, int DraftNotices, bool MustHandOver);

// The approved wording for an emergency termination. Nothing else is shown to passengers (REQ-OPS-008).
public static class EmergencyTemplates
{
    public const string Passenger = "Train {0} has stopped because of an emergency. Please follow staff instructions. Updates will appear here.";
}

internal static class IncidentViews
{
    public static IncidentRow Row(Incident i, IReadOnlyDictionary<string, string> people) =>
        new(i.Id, i.Title, i.Severity.ToString(), i.Status.ToString(), i.OwnerSubject, people.GetValueOrDefault(i.OwnerSubject), i.OpenedAtUtc, i.StationCodes);
}

// ---- reads ----

public sealed record ListIncidentsQuery(bool OpenOnly) : IQuery<IReadOnlyList<IncidentRow>>;

public sealed class ListIncidentsQueryHandler(IIncidentStore store, IStaffNames staff) : IQueryHandler<ListIncidentsQuery, IReadOnlyList<IncidentRow>>
{
    public async Task<Result<IReadOnlyList<IncidentRow>>> Handle(ListIncidentsQuery query, CancellationToken cancellationToken)
    {
        var list = await store.ListAsync(query.OpenOnly, cancellationToken);
        var people = await staff.GetNamesAsync(list.Select(i => i.OwnerSubject).Distinct(), cancellationToken);
        return Result.Success<IReadOnlyList<IncidentRow>>(list.Select(i => IncidentViews.Row(i, people)).ToList());
    }
}

public sealed record GetIncidentQuery(Guid Id) : IQuery<IncidentDetail>;

public sealed class GetIncidentQueryHandler(
    IIncidentStore incidents, IMessageStore messages, IDisruptionStore disruptions, IStaffNames staff, IDeviceHealth devices, IOpsJourneyStats journeys)
    : IQueryHandler<GetIncidentQuery, IncidentDetail>
{
    public async Task<Result<IncidentDetail>> Handle(GetIncidentQuery query, CancellationToken cancellationToken)
    {
        var i = await incidents.GetAsync(query.Id, cancellationToken);
        if (i is null)
            return OperationsErrors.IncidentNotFound(query.Id);

        var people = await staff.GetNamesAsync(i.Entries.Select(e => e.Author).Append(i.OwnerSubject).Distinct(), cancellationToken);
        var sent = await messages.ListForIncidentAsync(i.Id, cancellationToken);
        var displays = i.StationCodes.Count == 0 ? 0 : (await devices.GetSummaryAsync(cancellationToken)).ByType.Sum(t => t.Type is "PlatformDisplay" or "ConcourseBoard" ? t.Total : 0);

        var disruption = i.DisruptionId is null ? null : await disruptions.GetAsync(i.DisruptionId.Value, cancellationToken);
        var affected = i.StationCodes.Count == 0 ? 0 : await journeys.CountAffectedAsync(i.StationCodes, 2, cancellationToken);

        return new IncidentDetail(
            IncidentViews.Row(i, people), i.ReportedBy, i.ResolvedAtUtc, i.ResolutionNote,
            i.Entries.OrderByDescending(e => e.At).Select(e => new IncidentEntryView(e.At, e.Kind.ToString(), e.Text, e.Detail, e.Author, people.GetValueOrDefault(e.Author))).ToList(),
            new IncidentTold(null, displays, sent.Count, sent.Count(m => m.AcknowledgedAtUtc is not null)),
            new IncidentLinked(disruption?.Id, disruption?.Title, disruption?.ReviewByUtc, affected));
    }
}

public sealed record GetSignOutCheckQuery : IQuery<SignOutCheck>;

public sealed class GetSignOutCheckQueryHandler(IIncidentStore incidents, IDisruptionStore disruptions, IStaffNames staff, ICurrentUser user)
    : IQueryHandler<GetSignOutCheckQuery, SignOutCheck>
{
    public async Task<Result<SignOutCheck>> Handle(GetSignOutCheckQuery query, CancellationToken cancellationToken)
    {
        var me = user.UserId ?? string.Empty;
        var owned = (await incidents.ListAsync(true, cancellationToken)).Where(i => i.OwnerSubject == me).ToList();
        var people = await staff.GetNamesAsync([me], cancellationToken);
        var drafts = (await disruptions.ListOpenAsync(cancellationToken)).Count(d => d.CreatedBy == me && d.Status is DisruptionStatus.Draft or DisruptionStatus.Rejected);
        return new SignOutCheck(owned.Select(i => IncidentViews.Row(i, people)).ToList(), drafts, owned.Count > 0);
    }
}

// ---- declare ----

// Critical needs the critical permission (checked at the endpoint), a confirmation and a reason (REQ-OPS-007).
public sealed record DeclareIncidentCommand(
    string Title, string Severity, IReadOnlyList<string> StationCodes, string? ReportedBy, Guid? DisruptionId, bool Confirm, string? Reason)
    : ICommand<IncidentDetail>;

public sealed class DeclareIncidentCommandHandler(
    IIncidentStore store, IDisruptionStore disruptions, IStationLookup stationLookup, IOperationsUnitOfWork unitOfWork, IOperationsAuditLog audit,
    ICurrentUser user, TimeProvider clock, MediatR.ISender sender)
    : ICommandHandler<DeclareIncidentCommand, IncidentDetail>
{
    public async Task<Result<IncidentDetail>> Handle(DeclareIncidentCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IncidentSeverity>(command.Severity, true, out var severity))
            return Error.Validation("Operations.SeverityInvalid", "Severity is Minor, Major or Critical.");

        if (severity == IncidentSeverity.Critical)
        {
            if (!command.Confirm)
                return OperationsErrors.ConfirmationRequired;
            if (string.IsNullOrWhiteSpace(command.Reason))
                return OperationsErrors.ReasonRequired;
        }

        var codes = command.StationCodes.Select(c => c.Trim().ToUpperInvariant()).Where(c => c.Length > 0).Distinct().ToList();
        if (codes.Count > 0 && !await stationLookup.AllKnownAsync(codes, cancellationToken))
            return OperationsErrors.ScopeRequired;

        Disruption? linked = null;
        if (command.DisruptionId is { } did)
        {
            linked = await disruptions.GetAsync(did, cancellationToken);
            if (linked is null)
                return OperationsErrors.NotFound(did);
        }

        var now = clock.GetUtcNow();
        var actor = user.UserId ?? "unknown";
        var incident = Incident.Declare(command.Title, severity, actor, command.ReportedBy, codes, linked?.Id, now);
        if (incident.IsFailure)
            return incident.Error;

        if (linked is not null)
            incident.Value.LinkDisruption(linked.Id, linked.Title, actor, now);

        store.Add(incident.Value);
        audit.Record(new AuditRecord(actor, user.Roles.FirstOrDefault(), "incident.declare", "incident", incident.Value.Id.ToString(), command.Reason,
            After: new { incident.Value.Title, Severity = severity.ToString(), StationCodes = codes }));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await sender.Send(new GetIncidentQuery(incident.Value.Id), cancellationToken);
    }
}

// ---- notes, actions and handover ----

public sealed record AddIncidentNoteCommand(Guid Id, string Kind, string Text, string? Detail) : ICommand;

public sealed class AddIncidentNoteCommandHandler(IIncidentStore store, IOperationsUnitOfWork unitOfWork, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<AddIncidentNoteCommand>
{
    public async Task<Result> Handle(AddIncidentNoteCommand command, CancellationToken cancellationToken)
    {
        var i = await store.GetAsync(command.Id, cancellationToken);
        if (i is null)
            return OperationsErrors.IncidentNotFound(command.Id);

        var kind = string.Equals(command.Kind, "Action", StringComparison.OrdinalIgnoreCase) ? IncidentEntryKind.Action : IncidentEntryKind.Note;
        var result = i.AddNote(kind, command.Text, command.Detail, user.UserId ?? "unknown", clock.GetUtcNow());
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record LinkIncidentDisruptionCommand(Guid Id, Guid DisruptionId) : ICommand;

public sealed class LinkIncidentDisruptionCommandHandler(
    IIncidentStore incidents, IDisruptionStore disruptions, IOperationsUnitOfWork unitOfWork, IOperationsAuditLog audit, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<LinkIncidentDisruptionCommand>
{
    public async Task<Result> Handle(LinkIncidentDisruptionCommand command, CancellationToken cancellationToken)
    {
        var i = await incidents.GetAsync(command.Id, cancellationToken);
        if (i is null)
            return OperationsErrors.IncidentNotFound(command.Id);

        var d = await disruptions.GetAsync(command.DisruptionId, cancellationToken);
        if (d is null)
            return OperationsErrors.NotFound(command.DisruptionId);

        var actor = user.UserId ?? "unknown";
        var result = i.LinkDisruption(d.Id, d.Title, actor, clock.GetUtcNow());
        if (result.IsFailure)
            return result;

        audit.Record(new AuditRecord(actor, user.Roles.FirstOrDefault(), "incident.link_disruption", "incident", i.Id.ToString(), null, After: new { DisruptionId = d.Id }));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record HandOverIncidentCommand(Guid Id, string NewOwnerSubject, string Note) : ICommand;

public sealed class HandOverIncidentCommandHandler(
    IIncidentStore store, IStaffNames staff, IOperationsUnitOfWork unitOfWork, IOperationsAuditLog audit, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<HandOverIncidentCommand>
{
    public async Task<Result> Handle(HandOverIncidentCommand command, CancellationToken cancellationToken)
    {
        var i = await store.GetAsync(command.Id, cancellationToken);
        if (i is null)
            return OperationsErrors.IncidentNotFound(command.Id);

        var known = await staff.GetNamesAsync([command.NewOwnerSubject], cancellationToken);
        if (!known.ContainsKey(command.NewOwnerSubject))
            return OperationsErrors.NotAnOwnerCandidate;

        var actor = user.UserId ?? "unknown";
        var before = i.OwnerSubject;
        var result = i.HandOver(command.NewOwnerSubject, command.Note, actor, clock.GetUtcNow());
        if (result.IsFailure)
            return result;

        audit.Record(new AuditRecord(actor, user.Roles.FirstOrDefault(), "incident.handover", "incident", i.Id.ToString(), command.Note,
            Before: new { Owner = before }, After: new { Owner = command.NewOwnerSubject }));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record ResolveIncidentCommand(Guid Id, string Note) : ICommand;

public sealed class ResolveIncidentCommandHandler(IIncidentStore store, IOperationsUnitOfWork unitOfWork, IOperationsAuditLog audit, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<ResolveIncidentCommand>
{
    public async Task<Result> Handle(ResolveIncidentCommand command, CancellationToken cancellationToken)
    {
        var i = await store.GetAsync(command.Id, cancellationToken);
        if (i is null)
            return OperationsErrors.IncidentNotFound(command.Id);

        var actor = user.UserId ?? "unknown";
        var result = i.Resolve(command.Note, actor, clock.GetUtcNow());
        if (result.IsFailure)
            return result;

        audit.Record(new AuditRecord(actor, user.Roles.FirstOrDefault(), "incident.resolve", "incident", i.Id.ToString(), command.Note));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ---- emergency termination (REQ-OPS-008, CF-08) ----

// The journey ends where it is, its ETA becomes Unknown, its telemetry is kept as evidence, and the only public wording is the
// approved template. It needs a confirmation and a reason, and the incident becomes Critical.
public sealed record EmergencyTerminationCommand(Guid IncidentId, Guid JourneyId, bool Confirm, string Reason) : ICommand;

public sealed class EmergencyTerminationCommandHandler(
    IIncidentStore store, IJourneyControl control, IOpsJourneyStats journeys, IOperationsUnitOfWork unitOfWork, IOperationsAuditLog audit,
    ICurrentUser user, TimeProvider clock)
    : ICommandHandler<EmergencyTerminationCommand>
{
    public async Task<Result> Handle(EmergencyTerminationCommand command, CancellationToken cancellationToken)
    {
        if (!command.Confirm)
            return OperationsErrors.ConfirmationRequired;
        if (string.IsNullOrWhiteSpace(command.Reason))
            return OperationsErrors.ReasonRequired;

        var incident = await store.GetAsync(command.IncidentId, cancellationToken);
        if (incident is null)
            return OperationsErrors.IncidentNotFound(command.IncidentId);

        var facts = await journeys.GetFactsAsync(command.JourneyId, cancellationToken);
        if (facts is null)
            return Error.NotFound("Operations.JourneyNotFound", "That journey was not found.");

        var ended = await control.TerminateEmergencyAsync(command.JourneyId, command.Reason, cancellationToken);
        if (!ended.Succeeded)
            return Error.Conflict(ended.ErrorCode ?? "Operations.TerminationRefused", ended.Message ?? "The journey could not be terminated.");

        var now = clock.GetUtcNow();
        var actor = user.UserId ?? "unknown";
        incident.RaiseToCritical(actor, now);
        incident.AddNote(IncidentEntryKind.Action, $"Train {facts.TrainNumber} terminated in an emergency", command.Reason, actor, now);
        incident.AddNote(IncidentEntryKind.System, "Public message (approved template)", string.Format(EmergencyTemplates.Passenger, facts.TrainNumber), actor, now);

        audit.Record(new AuditRecord(actor, user.Roles.FirstOrDefault(), "incident.emergency_termination", "incident", incident.Id.ToString(), command.Reason,
            After: new { command.JourneyId, facts.TrainNumber }));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
