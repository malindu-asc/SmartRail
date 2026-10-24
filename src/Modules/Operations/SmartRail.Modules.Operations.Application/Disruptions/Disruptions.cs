using FluentValidation;
using SmartRail.Application.Core.Abstractions;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Journeys.Contracts;
using SmartRail.Modules.Network.Contracts;
using SmartRail.Modules.Operations.Application.Abstractions;
using SmartRail.Modules.Operations.Domain.Disruptions;
using SmartRail.Modules.Operations.Domain.Errors;
using SmartRail.SharedKernel.Abstractions;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Operations.Application.Disruptions;

public interface IDisruptionStore
{
    Task<Disruption?> GetAsync(Guid id, CancellationToken cancellationToken);

    // Published disruptions, newest first (the ones passengers see).
    Task<IReadOnlyList<Disruption>> ListPublishedAsync(CancellationToken cancellationToken);

    // Everything that is not closed, for the console: drafts, waiting, published and expired.
    Task<IReadOnlyList<Disruption>> ListOpenAsync(CancellationToken cancellationToken);

    // Everything the console may show: open ones, and resolved or retracted ones from the last weeks.
    Task<IReadOnlyList<Disruption>> ListForConsoleAsync(DateTimeOffset closedSince, CancellationToken cancellationToken);

    void Add(Disruption disruption);
}

// ---- What passengers see ----

public sealed record DisruptionSummary(
    Guid Id,
    string Title,
    string Message,
    string Severity,
    bool NetworkWide,
    IReadOnlyList<DisruptionStation> Stations,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ExpectedEndUtc,
    DateTimeOffset? StartsAtUtc = null,
    bool Planned = false);

public sealed record DisruptionStation(string Code, string Name);

public sealed record DisruptionDetail(DisruptionSummary Disruption, IReadOnlyList<DisruptionJourney> Journeys);

public sealed record DisruptionJourney(
    Guid JourneyId,
    string TrainNumber,
    string StationCode,
    string DestinationName,
    DateTimeOffset ScheduledDepartureUtc,
    DateTimeOffset ExpectedDepartureUtc,
    string Status,
    int? DelayMinutes,
    string? Platform);

internal static class DisruptionViews
{
    public static async Task<DisruptionSummary> SummaryAsync(Disruption d, INetworkStations stations, string? language, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var names = await stations.GetNamesAsync(d.StationCodes, language, cancellationToken);
        return new DisruptionSummary(
            d.Id, d.Title, d.Message, d.Severity.ToString(), d.NetworkWide,
            d.StationCodes.Select(c => new DisruptionStation(c, names.GetValueOrDefault(c, c))).ToList(),
            d.UpdatedAtUtc, d.ExpectedEndUtc, d.StartsAtUtc, d.IsPlanned(now));
    }

    // How a notice reads where passengers see it. Plain text only, never markup (REQ-SEC-005).
    public static DisruptionPreview Preview(string title, string message, string severity, DateTimeOffset now, Func<DateTimeOffset, string> hm)
    {
        var first = message.Trim().Split(['.', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? message.Trim();
        var line = first.Length <= 90 ? first : first[..87] + "...";
        var display = $"{title.Trim()}: {char.ToLowerInvariant(line[0])}{line[1..]}";
        return new DisruptionPreview(
            new AppPreview(title.Trim(), $"{line} · updated {hm(now)}", severity),
            display.Length <= 120 ? display : display[..117] + "...",
            display.Length <= 160 ? display : display[..157] + "...");
    }
}

public sealed record DisruptionPreview(AppPreview App, string PlatformDisplay, string Terminal);

public sealed record AppPreview(string Title, string Detail, string Severity);

public sealed record ListDisruptionsQuery(string? Language) : IQuery<IReadOnlyList<DisruptionSummary>>;

public sealed class ListDisruptionsQueryHandler(IDisruptionStore store, INetworkStations stations, TimeProvider clock)
    : IQueryHandler<ListDisruptionsQuery, IReadOnlyList<DisruptionSummary>>
{
    public async Task<Result<IReadOnlyList<DisruptionSummary>>> Handle(ListDisruptionsQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var list = new List<DisruptionSummary>();
        foreach (var d in (await store.ListPublishedAsync(cancellationToken)).Where(d => !d.IsOver(now)))
            list.Add(await DisruptionViews.SummaryAsync(d, stations, query.Language, now, cancellationToken));

        return Result.Success<IReadOnlyList<DisruptionSummary>>(list);
    }
}

public sealed record GetDisruptionQuery(Guid Id, string? Language) : IQuery<DisruptionDetail>;

public sealed class GetDisruptionQueryHandler(IDisruptionStore store, INetworkStations stations, IAffectedJourneys affected, TimeProvider clock)
    : IQueryHandler<GetDisruptionQuery, DisruptionDetail>
{
    public async Task<Result<DisruptionDetail>> Handle(GetDisruptionQuery query, CancellationToken cancellationToken)
    {
        var d = await store.GetAsync(query.Id, cancellationToken);

        // A draft, a rejected, a resolved or a retracted disruption is not public: it looks the same as one that never existed.
        if (d is null || d.Status != DisruptionStatus.Published)
            return OperationsErrors.NotFound(query.Id);

        var journeys = d.NetworkWide || d.StationCodes.Count == 0
            ? []
            : await affected.FindAsync(d.StationCodes, 10, query.Language, cancellationToken);

        return new DisruptionDetail(
            await DisruptionViews.SummaryAsync(d, stations, query.Language, clock.GetUtcNow(), cancellationToken),
            journeys.Select(j => new DisruptionJourney(
                j.JourneyId, j.TrainNumber, j.StationCode, j.DestinationName, j.ScheduledDepartureUtc,
                j.ExpectedDepartureUtc, j.Status, j.DelayMinutes, j.Platform)).ToList());
    }
}

// ---- The disruption console (UC-OPS-04, REQ-OPS-005, REQ-OPS-006, REQ-OPS-016) ----

public sealed record DisruptionRow(
    Guid Id,
    string Title,
    string Scope,
    string Severity,
    string Status,
    string? Type,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset ReviewByUtc,
    bool ReviewExpired,
    DateTimeOffset? StartsAtUtc,
    bool Planned,
    string OwnerSubject,
    string? OwnerName);

public sealed record DisruptionCounts(int Active, int AwaitingApproval, int Drafts, int Resolved, int Expired);

public sealed record DisruptionConsoleList(DisruptionCounts Counts, IReadOnlyList<DisruptionRow> Items);

public sealed record DisruptionHistoryEntry(DateTimeOffset At, string Text, string? Person);

public sealed record DisruptionConsoleDetail(
    Guid Id,
    string Title,
    string Message,
    string Severity,
    string Status,
    string? Type,
    bool NetworkWide,
    IReadOnlyList<DisruptionStation> Stations,
    DateTimeOffset? ExpectedEndUtc,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset ReviewByUtc,
    bool ReviewExpired,
    string OwnerSubject,
    string? OwnerName,
    string? ApprovedBy,
    string? RejectedBy,
    string? RejectionComment,
    int? JourneysAffected,
    DisruptionPreview Preview,
    IReadOnlyList<DisruptionHistoryEntry> History,
    bool CanApprove);

// tab: active | awaiting | drafts | resolved | all
public sealed record ListDisruptionsConsoleQuery(string? Tab) : IQuery<DisruptionConsoleList>;

public sealed class ListDisruptionsConsoleQueryHandler(IDisruptionStore store, INetworkStations stations, IStaffNames staff, TimeProvider clock)
    : IQueryHandler<ListDisruptionsConsoleQuery, DisruptionConsoleList>
{
    public async Task<Result<DisruptionConsoleList>> Handle(ListDisruptionsConsoleQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var all = await store.ListForConsoleAsync(now.AddDays(-30), cancellationToken);

        var counts = new DisruptionCounts(
            all.Count(d => d.Status == DisruptionStatus.Published),
            all.Count(d => d.Status == DisruptionStatus.AwaitingApproval),
            all.Count(d => d.Status is DisruptionStatus.Draft or DisruptionStatus.Rejected),
            all.Count(d => d.Status is DisruptionStatus.Resolved or DisruptionStatus.Retracted),
            all.Count(d => d.IsExpired(now)));

        var tab = (query.Tab ?? "active").ToLowerInvariant();
        var shown = tab switch
        {
            "awaiting" => all.Where(d => d.Status == DisruptionStatus.AwaitingApproval),
            "drafts" => all.Where(d => d.Status is DisruptionStatus.Draft or DisruptionStatus.Rejected),
            "resolved" => all.Where(d => d.Status is DisruptionStatus.Resolved or DisruptionStatus.Retracted),
            "planned" => all.Where(d => d.StartsAtUtc > now && d.Status is not (DisruptionStatus.Resolved or DisruptionStatus.Retracted)),
            "all" => all,
            _ => all.Where(d => d.Status == DisruptionStatus.Published)
        };

        var list = shown.OrderByDescending(d => d.UpdatedAtUtc).ToList();
        var names = await stations.GetNamesAsync(list.SelectMany(d => d.StationCodes).Distinct(), null, cancellationToken);
        var people = await staff.GetNamesAsync(list.Select(d => d.CreatedBy).Distinct(), cancellationToken);

        var rows = list.Select(d => new DisruptionRow(
            d.Id, d.Title, d.NetworkWide ? "All stations" : string.Join(", ", d.StationCodes.Select(c => names.GetValueOrDefault(c, c))),
            d.Severity.ToString(), d.Status.ToString(), d.Type, d.PublishedAtUtc, d.ReviewByUtc, d.IsExpired(now), d.StartsAtUtc, d.IsPlanned(now),
            d.CreatedBy, people.GetValueOrDefault(d.CreatedBy))).ToList();

        return new DisruptionConsoleList(counts, rows);
    }
}

public sealed record GetDisruptionConsoleQuery(Guid Id) : IQuery<DisruptionConsoleDetail>;

public sealed class GetDisruptionConsoleQueryHandler(
    IDisruptionStore store, INetworkStations stations, IStaffNames staff, IOpsJourneyStats journeys, INetworkTimeZone timeZone, ICurrentUser user, TimeProvider clock)
    : IQueryHandler<GetDisruptionConsoleQuery, DisruptionConsoleDetail>
{
    public async Task<Result<DisruptionConsoleDetail>> Handle(GetDisruptionConsoleQuery query, CancellationToken cancellationToken)
    {
        var d = await store.GetAsync(query.Id, cancellationToken);
        if (d is null)
            return OperationsErrors.NotFound(query.Id);

        var now = clock.GetUtcNow();
        var names = await stations.GetNamesAsync(d.StationCodes, null, cancellationToken);
        var people = await staff.GetNamesAsync([d.CreatedBy, d.ApprovedBy ?? string.Empty, d.RejectedBy ?? string.Empty], cancellationToken);
        string? Person(string? s) => s is null ? null : people.GetValueOrDefault(s, s);

        int? affected = d.NetworkWide ? null : await journeys.CountAffectedAsync(d.StationCodes, 2, cancellationToken);

        var history = new List<DisruptionHistoryEntry> { new(d.CreatedAtUtc, "Draft created", Person(d.CreatedBy)) };
        if (d.SubmittedAtUtc is { } submitted)
            history.Add(new(submitted, "Published", Person(d.CreatedBy)));
        if (d.RejectedBy is not null)
            history.Add(new(d.UpdatedAtUtc, $"Rejected: {d.RejectionComment}", Person(d.RejectedBy)));
        if (d.ApprovedBy is not null && d.PublishedAtUtc is { } published)
            history.Add(new(published, "Approved and published", Person(d.ApprovedBy)));
        if (d.Status is DisruptionStatus.Resolved or DisruptionStatus.Retracted)
            history.Add(new(d.UpdatedAtUtc, d.Status == DisruptionStatus.Resolved ? "Resolved" : "Retracted", null));

        return new DisruptionConsoleDetail(
            d.Id, d.Title, d.Message, d.Severity.ToString(), d.Status.ToString(), d.Type, d.NetworkWide,
            d.StationCodes.Select(c => new DisruptionStation(c, names.GetValueOrDefault(c, c))).ToList(),
            d.ExpectedEndUtc, d.StartsAtUtc, d.ReviewByUtc, d.IsExpired(now), d.CreatedBy, Person(d.CreatedBy), Person(d.ApprovedBy),
            Person(d.RejectedBy), d.RejectionComment, affected,
            DisruptionViews.Preview(d.Title, d.Message, d.Severity.ToString(), d.UpdatedAtUtc, t => timeZone.Hm(t)),
            history.OrderBy(h => h.At).ToList(),
            d.Status == DisruptionStatus.AwaitingApproval && !string.Equals(d.CreatedBy, user.UserId, StringComparison.Ordinal));
    }
}

internal static class DisruptionTime
{
    public static string Hm(this INetworkTimeZone zone, DateTimeOffset t) => TimeZoneInfo.ConvertTime(t, zone.Zone).ToString("HH:mm");
}

// How the notice will look, before it is saved (the "Passenger preview" panel).
public sealed record PreviewDisruptionQuery(string Title, string Message, string Severity) : IQuery<DisruptionPreview>;

public sealed class PreviewDisruptionQueryHandler(INetworkTimeZone timeZone, TimeProvider clock) : IQueryHandler<PreviewDisruptionQuery, DisruptionPreview>
{
    public Task<Result<DisruptionPreview>> Handle(PreviewDisruptionQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Title) || string.IsNullOrWhiteSpace(query.Message))
            return Task.FromResult<Result<DisruptionPreview>>(OperationsErrors.TextInvalid);

        return Task.FromResult(Result.Success(DisruptionViews.Preview(query.Title, query.Message, query.Severity, clock.GetUtcNow(), t => timeZone.Hm(t))));
    }
}

// Create, or change a draft or a live local notice. Submit false saves a draft; true publishes it, or sends a network-wide notice to
// a second approver. A notice that starts in the future is planned works (REQ-OPS-016).
public sealed record SaveDisruptionCommand(
    Guid? Id, string Title, string Message, string Severity, bool NetworkWide, IReadOnlyList<string> StationCodes, string? Type,
    DateTimeOffset? StartsAtUtc, DateTimeOffset? ExpectedEndUtc, DateTimeOffset ReviewByUtc, bool Submit) : ICommand<DisruptionConsoleDetail>;

public sealed class SaveDisruptionCommandValidator : AbstractValidator<SaveDisruptionCommand>
{
    public SaveDisruptionCommandValidator()
    {
        RuleFor(x => x.Severity).Must(s => Enum.TryParse<DisruptionSeverity>(s, true, out _)).WithMessage("Severity is Minor, Major or Severe.");
    }
}

public sealed class SaveDisruptionCommandHandler(
    IDisruptionStore store, IStationLookup stationLookup, IOperationsUnitOfWork unitOfWork, IOperationsAuditLog audit, ICurrentUser user,
    TimeProvider clock, MediatR.ISender sender)
    : ICommandHandler<SaveDisruptionCommand, DisruptionConsoleDetail>
{
    public async Task<Result<DisruptionConsoleDetail>> Handle(SaveDisruptionCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var actor = user.UserId ?? "unknown";
        var severity = Enum.Parse<DisruptionSeverity>(command.Severity, true);

        var codes = command.StationCodes.Select(c => c.Trim().ToUpperInvariant()).Where(c => c.Length > 0).Distinct().ToList();
        if (!command.NetworkWide && !await stationLookup.AllKnownAsync(codes, cancellationToken))
            return OperationsErrors.ScopeRequired;

        Disruption disruption;
        string action;

        if (command.Id is { } id)
        {
            var existing = await store.GetAsync(id, cancellationToken);
            if (existing is null)
                return OperationsErrors.NotFound(id);

            var updated = existing.Update(command.Title, command.Message, severity, command.ExpectedEndUtc, command.ReviewByUtc, now, codes, command.Type, command.StartsAtUtc);
            if (updated.IsFailure)
                return updated.Error;

            if (command.Submit)
            {
                var sent = existing.Submit(now);
                if (sent.IsFailure && existing.Status is DisruptionStatus.Draft or DisruptionStatus.Rejected)
                    return sent.Error;
            }

            disruption = existing;
            action = "disruption.update";
        }
        else
        {
            var created = Disruption.Create(command.Title, command.Message, severity, command.NetworkWide, codes, command.ExpectedEndUtc, command.ReviewByUtc,
                actor, now, null, command.Type, command.StartsAtUtc, command.Submit);
            if (created.IsFailure)
                return created.Error;

            disruption = created.Value;
            store.Add(disruption);
            action = command.Submit ? "disruption.publish" : "disruption.draft";
        }

        audit.Record(new AuditRecord(actor, user.Roles.FirstOrDefault(), action, "disruption", disruption.Id.ToString(), null,
            After: new { disruption.Title, disruption.Severity, disruption.NetworkWide, disruption.StationCodes, Status = disruption.Status.ToString(), disruption.StartsAtUtc }));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await sender.Send(new GetDisruptionConsoleQuery(disruption.Id), cancellationToken);
    }
}

public sealed record SubmitDisruptionCommand(Guid Id) : ICommand;

public sealed class SubmitDisruptionCommandHandler(IDisruptionStore store, IOperationsUnitOfWork unitOfWork, IOperationsAuditLog audit, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<SubmitDisruptionCommand>
{
    public async Task<Result> Handle(SubmitDisruptionCommand command, CancellationToken cancellationToken)
    {
        var d = await store.GetAsync(command.Id, cancellationToken);
        if (d is null)
            return OperationsErrors.NotFound(command.Id);

        var result = d.Submit(clock.GetUtcNow());
        if (result.IsFailure)
            return result;

        audit.Record(new AuditRecord(user.UserId ?? "unknown", user.Roles.FirstOrDefault(), "disruption.publish", "disruption", d.Id.ToString(), null));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public enum DisruptionAction { Approve, Resolve, Retract }

public sealed record ActOnDisruptionCommand(Guid Id, DisruptionAction Action, string? Reason) : ICommand;

public sealed class ActOnDisruptionCommandHandler(IDisruptionStore store, IOperationsUnitOfWork unitOfWork, IOperationsAuditLog audit, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<ActOnDisruptionCommand>
{
    public async Task<Result> Handle(ActOnDisruptionCommand command, CancellationToken cancellationToken)
    {
        var d = await store.GetAsync(command.Id, cancellationToken);
        if (d is null)
            return OperationsErrors.NotFound(command.Id);

        var now = clock.GetUtcNow();
        var actor = user.UserId ?? "unknown";
        var result = command.Action switch
        {
            DisruptionAction.Approve => d.Approve(actor, now),
            DisruptionAction.Resolve => d.Resolve(now),
            _ => d.Retract(now)
        };
        if (result.IsFailure)
            return result;

        audit.Record(new AuditRecord(actor, user.Roles.FirstOrDefault(), $"disruption.{command.Action.ToString().ToLowerInvariant()}", "disruption", d.Id.ToString(), command.Reason,
            After: new { d.Title, Status = d.Status.ToString() }));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record RejectDisruptionCommand(Guid Id, string Comment) : ICommand;

public sealed class RejectDisruptionCommandHandler(IDisruptionStore store, IOperationsUnitOfWork unitOfWork, IOperationsAuditLog audit, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<RejectDisruptionCommand>
{
    public async Task<Result> Handle(RejectDisruptionCommand command, CancellationToken cancellationToken)
    {
        var d = await store.GetAsync(command.Id, cancellationToken);
        if (d is null)
            return OperationsErrors.NotFound(command.Id);

        var actor = user.UserId ?? "unknown";
        var result = d.Reject(actor, command.Comment, clock.GetUtcNow());
        if (result.IsFailure)
            return result;

        audit.Record(new AuditRecord(actor, user.Roles.FirstOrDefault(), "disruption.reject", "disruption", d.Id.ToString(), command.Comment));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// Station codes must exist before a notice names them.
public interface IStationLookup
{
    Task<bool> AllKnownAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken);
}

public sealed class StationLookup(INetworkStations stations) : IStationLookup
{
    public async Task<bool> AllKnownAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        if (codes.Count == 0)
            return false;

        var known = await stations.GetNamesAsync(codes, null, cancellationToken);
        return codes.All(known.ContainsKey);
    }
}
