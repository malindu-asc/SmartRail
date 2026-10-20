using FluentValidation;
using SmartRail.Application.Core.Abstractions;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Contracts;
using SmartRail.Modules.Network.Domain.Errors;
using SmartRail.Modules.Network.Domain.Routes;
using SmartRail.Modules.Network.Domain.Timetables;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Application.Timetables.Versions;

// The scheduler's timetable workflow (UC-OPS-05, REQ-OPS-003): draft, edit, submit, a different person approves, the old version is
// superseded. Published and superseded versions never change. Journeys already generated are untouched (REQ-LIVE-017); the generator
// reads the version in force on each date, so a new version only shapes journeys from its effective date.

public interface INetworkAuditLog : IAuditLog;

public sealed record VersionListItem(Guid Id, string Label, string Status, DateOnly? EffectiveFrom, string Author, string? ApprovedBy, int Services, DateTimeOffset CreatedAtUtc, DateTimeOffset? PublishedAtUtc);

public interface ITimetableVersionStore
{
    Task<IReadOnlyList<VersionListItem>> ListAsync(CancellationToken cancellationToken);

    // With services and stops, tracked for update.
    Task<TimetableVersion?> GetAsync(Guid id, CancellationToken cancellationToken);

    // Same, but not tracked (for reading and comparing).
    Task<TimetableVersion?> GetReadOnlyAsync(Guid id, CancellationToken cancellationToken);

    // The published version in force on a date, with services and stops, not tracked. Null before the first one.
    Task<TimetableVersion?> GetInForceAsync(DateOnly date, CancellationToken cancellationToken);

    Task<bool> HasOpenDraftAsync(CancellationToken cancellationToken);

    Task<bool> EffectiveDateTakenAsync(DateOnly date, CancellationToken cancellationToken);

    // Published versions that came into force before the date (the ones a new version supersedes), tracked.
    Task<IReadOnlyList<TimetableVersion>> GetPublishedBeforeAsync(DateOnly date, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, Route>> GetRoutesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);

    Task<Route?> GetRouteByCodeAsync(string code, CancellationToken cancellationToken);

    Task<Guid?> GetCalendarIdAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, string>> GetCalendarCodesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);

    // Station id to (code, name).
    Task<IReadOnlyDictionary<Guid, (string Code, string Name)>> GetStationsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, Guid>> GetStationIdsAsync(IEnumerable<string> codes, CancellationToken cancellationToken);

    void Add(TimetableVersion version);

    void AddService(Service service);

    void RemoveService(Service service);
}

// ---- views ----

public sealed record VersionRow(
    Guid Id, string Label, string Status, DateOnly? EffectiveFrom, string Author, string? AuthorName, string? ApprovedBy, string? ApprovedByName,
    int Services, DateTimeOffset CreatedAtUtc, DateTimeOffset? PublishedAtUtc, bool InForce);

public sealed record VersionList(IReadOnlyList<VersionRow> Items);

public sealed record VersionStopView(int Sequence, string StationCode, string StationName, string? Arrival, string? Departure, int? ArrivalMinutes, int? DepartureMinutes, string Kind);

public sealed record VersionServiceView(
    Guid Id, string TrainNumber, string RouteCode, string Direction, string Category, string CalendarCode, string Origin, string Destination,
    string? FirstDeparture, string? LastArrival, IReadOnlyList<VersionStopView> Stops);

// What the signed-in person may do with this version right now (the buttons the console shows).
public sealed record VersionActions(bool CanEdit, bool CanSubmit, bool CanApprove, string? ApproveBlockedReason);

public sealed record VersionDetail(VersionRow Version, IReadOnlyList<VersionServiceView> Services, VersionActions Actions);

internal static class VersionViews
{
    public static string Hm(int minutes) => $"{minutes / 60:00}:{minutes % 60:00}";

    public static async Task<VersionRow> RowAsync(TimetableVersion v, int services, bool inForce, IStaffNames staff, CancellationToken ct)
    {
        var names = await staff.GetNamesAsync(new[] { v.Author, v.ApprovedBy }.Where(s => s is not null).Select(s => s!), ct);
        return new VersionRow(
            v.Id, v.Label, v.Status.ToString(), v.EffectiveFrom, v.Author, names.GetValueOrDefault(v.Author), v.ApprovedBy,
            v.ApprovedBy is null ? null : names.GetValueOrDefault(v.ApprovedBy), services, v.CreatedAtUtc, v.PublishedAt, inForce);
    }

    public static async Task<IReadOnlyList<VersionServiceView>> ServicesAsync(TimetableVersion v, ITimetableVersionStore store, CancellationToken ct)
    {
        var routes = await store.GetRoutesAsync(v.Services.Select(s => s.RouteId), ct);
        var calendars = await store.GetCalendarCodesAsync(v.Services.Select(s => s.CalendarId), ct);
        var stations = await store.GetStationsAsync(v.Services.SelectMany(s => s.Stops).Select(st => st.StationId).Distinct(), ct);

        return v.Services.OrderBy(s => s.Stops.Min(st => st.DepartureMinutes ?? st.ArrivalMinutes ?? 0)).ThenBy(s => s.TrainNumber).Select(s =>
        {
            var stops = s.Stops.OrderBy(st => st.Sequence).Select(st =>
            {
                var station = stations.GetValueOrDefault(st.StationId, ("?", "?"));
                return new VersionStopView(st.Sequence, station.Item1, station.Item2,
                    st.ArrivalMinutes is { } a ? Hm(a) : null, st.DepartureMinutes is { } d ? Hm(d) : null, st.ArrivalMinutes, st.DepartureMinutes, st.Kind.ToString());
            }).ToList();

            return new VersionServiceView(
                s.Id, s.TrainNumber, routes.TryGetValue(s.RouteId, out var r) ? r.Code : "?", s.Direction.ToString(), s.Category.ToString(),
                calendars.GetValueOrDefault(s.CalendarId, "?"), stops[0].StationName, stops[^1].StationName, stops[0].Departure, stops[^1].Arrival, stops);
        }).ToList();
    }

    public static VersionActions Actions(TimetableVersion v, string? me)
    {
        var editable = v.Status is TimetableVersionStatus.Draft or TimetableVersionStatus.InReview;
        var isAuthor = string.Equals(v.Author, me, StringComparison.OrdinalIgnoreCase);
        var canApprove = v.Status == TimetableVersionStatus.InReview && !isAuthor;
        return new VersionActions(
            editable, v.Status == TimetableVersionStatus.Draft && v.Services.Count > 0, canApprove,
            v.Status == TimetableVersionStatus.InReview && isAuthor ? "A different person must approve this version." : null);
    }
}

// ---- reads ----

public sealed record ListTimetableVersionsQuery : IQuery<VersionList>;

public sealed class ListTimetableVersionsQueryHandler(ITimetableVersionStore store, IStaffNames staff, INetworkTimeZone timeZone)
    : IQueryHandler<ListTimetableVersionsQuery, VersionList>
{
    public async Task<Result<VersionList>> Handle(ListTimetableVersionsQuery query, CancellationToken cancellationToken)
    {
        var items = await store.ListAsync(cancellationToken);
        var names = await staff.GetNamesAsync(items.SelectMany(i => new[] { i.Author, i.ApprovedBy }).Where(s => s is not null).Select(s => s!).Distinct(), cancellationToken);
        var today = timeZone.Today();

        var inForce = VersionResolver.Resolve(items.Select(i => new TimetableVersionSummary(i.Id, Enum.Parse<TimetableVersionStatus>(i.Status), i.EffectiveFrom)), today)?.Id;

        return new VersionList(items
            .OrderBy(i => i.Status is "Draft" or "InReview" ? 0 : 1).ThenByDescending(i => i.EffectiveFrom).ThenByDescending(i => i.CreatedAtUtc)
            .Select(i => new VersionRow(i.Id, i.Label, i.Status, i.EffectiveFrom, i.Author, names.GetValueOrDefault(i.Author), i.ApprovedBy,
                i.ApprovedBy is null ? null : names.GetValueOrDefault(i.ApprovedBy), i.Services, i.CreatedAtUtc, i.PublishedAtUtc, i.Id == inForce)).ToList());
    }
}

public sealed record GetTimetableVersionQuery(Guid Id) : IQuery<VersionDetail>;

public sealed class GetTimetableVersionQueryHandler(ITimetableVersionStore store, IStaffNames staff, INetworkTimeZone timeZone, ICurrentUser user)
    : IQueryHandler<GetTimetableVersionQuery, VersionDetail>
{
    public async Task<Result<VersionDetail>> Handle(GetTimetableVersionQuery query, CancellationToken cancellationToken)
    {
        var version = await store.GetReadOnlyAsync(query.Id, cancellationToken);
        if (version is null)
            return NetworkErrors.VersionNotFound(query.Id);

        var inForce = await store.GetInForceAsync(timeZone.Today(), cancellationToken);
        var row = await VersionViews.RowAsync(version, version.Services.Count, inForce?.Id == version.Id, staff, cancellationToken);
        return new VersionDetail(row, await VersionViews.ServicesAsync(version, store, cancellationToken), VersionViews.Actions(version, user.UserId));
    }
}

// What changed against another version (default: the version in force today), by train number.
public sealed record CompareChange(string TrainNumber, string Kind, string Summary);

public sealed record VersionComparison(Guid VersionId, Guid AgainstId, string AgainstLabel, int Added, int Removed, int Changed, int Unchanged, IReadOnlyList<CompareChange> Changes);

public sealed record CompareTimetableVersionQuery(Guid Id, Guid? AgainstId) : IQuery<VersionComparison>;

public sealed class CompareTimetableVersionQueryHandler(ITimetableVersionStore store, INetworkTimeZone timeZone)
    : IQueryHandler<CompareTimetableVersionQuery, VersionComparison>
{
    public async Task<Result<VersionComparison>> Handle(CompareTimetableVersionQuery query, CancellationToken cancellationToken)
    {
        var version = await store.GetReadOnlyAsync(query.Id, cancellationToken);
        if (version is null)
            return NetworkErrors.VersionNotFound(query.Id);

        var against = query.AgainstId is { } id ? await store.GetReadOnlyAsync(id, cancellationToken) : await store.GetInForceAsync(timeZone.Today(), cancellationToken);
        if (against is null)
            return query.AgainstId is { } missing ? NetworkErrors.VersionNotFound(missing) : NetworkErrors.NothingToBaseOn;

        var stations = await store.GetStationsAsync(version.Services.Concat(against.Services).SelectMany(s => s.Stops).Select(s => s.StationId).Distinct(), cancellationToken);
        var mine = version.Services.ToDictionary(s => s.TrainNumber);
        var theirs = against.Services.ToDictionary(s => s.TrainNumber);
        var changes = new List<CompareChange>();
        var unchanged = 0;

        foreach (var (number, service) in mine.OrderBy(p => p.Key))
        {
            if (!theirs.TryGetValue(number, out var old))
            {
                changes.Add(new CompareChange(number, "Added", Describe(service, stations)));
                continue;
            }

            var diff = Differences(old, service, stations);
            if (diff.Count == 0)
                unchanged++;
            else
                changes.Add(new CompareChange(number, "Changed", string.Join("; ", diff)));
        }

        foreach (var (number, service) in theirs.Where(p => !mine.ContainsKey(p.Key)).OrderBy(p => p.Key))
            changes.Add(new CompareChange(number, "Removed", Describe(service, stations)));

        return new VersionComparison(
            version.Id, against.Id, against.Label, changes.Count(c => c.Kind == "Added"), changes.Count(c => c.Kind == "Removed"),
            changes.Count(c => c.Kind == "Changed"), unchanged, changes);
    }

    private static string Describe(Service s, IReadOnlyDictionary<Guid, (string Code, string Name)> stations)
    {
        var stops = s.Stops.OrderBy(x => x.Sequence).ToList();
        var first = stops[0];
        var last = stops[^1];
        return $"{stations.GetValueOrDefault(first.StationId).Name} {VersionViews.Hm(first.DepartureMinutes ?? 0)} to {stations.GetValueOrDefault(last.StationId).Name} {VersionViews.Hm(last.ArrivalMinutes ?? 0)}";
    }

    private static List<string> Differences(Service old, Service now, IReadOnlyDictionary<Guid, (string Code, string Name)> stations)
    {
        var diff = new List<string>();
        if (old.RouteId != now.RouteId)
            diff.Add("route changed");
        if (old.CalendarId != now.CalendarId)
            diff.Add("days changed");

        var a = old.Stops.OrderBy(x => x.Sequence).ToList();
        var b = now.Stops.OrderBy(x => x.Sequence).ToList();
        if (!a.Select(x => x.StationId).SequenceEqual(b.Select(x => x.StationId)))
        {
            diff.Add("stops changed");
            return diff;
        }

        for (var i = 0; i < a.Count; i++)
        {
            if (a[i].ArrivalMinutes != b[i].ArrivalMinutes || a[i].DepartureMinutes != b[i].DepartureMinutes)
            {
                var from = VersionViews.Hm(a[i].DepartureMinutes ?? a[i].ArrivalMinutes ?? 0);
                var to = VersionViews.Hm(b[i].DepartureMinutes ?? b[i].ArrivalMinutes ?? 0);
                diff.Add($"{stations.GetValueOrDefault(a[i].StationId).Name} {from} to {to}");
            }
        }

        return diff;
    }
}

// ---- writes ----

// A draft starts as a copy of the version in force. Only one draft at a time, so two people do not publish over each other.
public sealed record CreateTimetableDraftCommand(string Label) : ICommand<VersionDetail>;

public sealed class CreateTimetableDraftCommandValidator : AbstractValidator<CreateTimetableDraftCommand>
{
    public CreateTimetableDraftCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateTimetableDraftCommandHandler(
    ITimetableVersionStore store, INetworkUnitOfWork unitOfWork, INetworkAuditLog audit, IStaffNames staff, INetworkTimeZone timeZone, ICurrentUser user)
    : ICommandHandler<CreateTimetableDraftCommand, VersionDetail>
{
    public async Task<Result<VersionDetail>> Handle(CreateTimetableDraftCommand command, CancellationToken cancellationToken)
    {
        var me = user.UserId ?? "unknown";

        if (await store.HasOpenDraftAsync(cancellationToken))
            return NetworkErrors.DraftAlreadyOpen;

        var source = await store.GetInForceAsync(timeZone.Today(), cancellationToken);
        if (source is null)
            return NetworkErrors.NothingToBaseOn;

        var draft = TimetableVersion.CreateDraft(command.Label, me);
        if (draft.IsFailure)
            return draft.Error;

        var routes = await store.GetRoutesAsync(source.Services.Select(s => s.RouteId), cancellationToken);
        foreach (var service in source.Services)
        {
            var copy = Service.Create(
                draft.Value.Id, routes[service.RouteId], service.CalendarId, service.TrainNumber, service.Direction, service.Category,
                service.Stops.OrderBy(s => s.Sequence).Select(s => new ServiceStopDraft(s.Sequence, s.StationId, s.ArrivalMinutes, s.DepartureMinutes, s.DefaultPlatformId, s.Kind)).ToList());
            if (copy.IsFailure)
                return copy.Error;

            draft.Value.AddService(copy.Value);
            store.AddService(copy.Value);
        }

        store.Add(draft.Value);
        audit.Record(new AuditRecord(me, user.Roles.FirstOrDefault(), "timetable.draft", "timetable_version", draft.Value.Id.ToString(), null, null,
            new { draft.Value.Label, BasedOn = source.Label, Services = draft.Value.Services.Count }));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var row = await VersionViews.RowAsync(draft.Value, draft.Value.Services.Count, false, staff, cancellationToken);
        return new VersionDetail(row, await VersionViews.ServicesAsync(draft.Value, store, cancellationToken), VersionViews.Actions(draft.Value, me));
    }
}

public sealed record StopTimeInput(string StationCode, string? Arrival, string? Departure);

// Change the times of one service in a draft. The stops stay as they are; give a time per stop, in order.
public sealed record ChangeServiceTimesCommand(Guid VersionId, Guid ServiceId, IReadOnlyList<StopTimeInput> Stops) : ICommand;

public sealed class ChangeServiceTimesCommandHandler(
    ITimetableVersionStore store, INetworkUnitOfWork unitOfWork, INetworkAuditLog audit, ICurrentUser user)
    : ICommandHandler<ChangeServiceTimesCommand>
{
    public async Task<Result> Handle(ChangeServiceTimesCommand command, CancellationToken cancellationToken)
    {
        var version = await store.GetAsync(command.VersionId, cancellationToken);
        if (version is null)
            return NetworkErrors.VersionNotFound(command.VersionId);

        var service = version.Services.FirstOrDefault(s => s.Id == command.ServiceId);
        if (service is null)
            return NetworkErrors.ServiceNotInVersion(command.ServiceId);

        var stations = await store.GetStationsAsync(service.Stops.Select(s => s.StationId), cancellationToken);
        var ordered = service.Stops.OrderBy(s => s.Sequence).ToList();
        if (command.Stops.Count != ordered.Count
            || !ordered.Select(s => stations.GetValueOrDefault(s.StationId).Code).SequenceEqual(command.Stops.Select(s => s.StationCode.Trim().ToUpperInvariant())))
            return NetworkErrors.StopTimesMismatch;

        var times = new List<ServiceStopTime>();
        foreach (var stop in command.Stops)
        {
            if (!TryMinutes(stop.Arrival, out var arrival) || !TryMinutes(stop.Departure, out var departure))
                return NetworkErrors.ServiceTimesInvalid;

            times.Add(new ServiceStopTime(arrival, departure));
        }

        var before = ordered.Select(s => new { Station = stations.GetValueOrDefault(s.StationId).Code, s.ArrivalMinutes, s.DepartureMinutes }).ToList();
        var changed = version.ChangeServiceTimes(command.ServiceId, times);
        if (changed.IsFailure)
            return changed;

        audit.Record(new AuditRecord(user.UserId ?? "unknown", user.Roles.FirstOrDefault(), "timetable.service.times", "timetable_version", version.Id.ToString(), null,
            before, new { service.TrainNumber, Stops = command.Stops }));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // "HH:mm", hours may pass 24 (24:25) for a service that runs past midnight; empty means no time at this stop.
    internal static bool TryMinutes(string? text, out int? minutes)
    {
        minutes = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var parts = text.Trim().Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var h) || !int.TryParse(parts[1], out var m) || h < 0 || m is < 0 or > 59)
            return false;

        minutes = h * 60 + m;
        return true;
    }
}

public sealed record AddServiceCommand(
    Guid VersionId, string TrainNumber, string RouteCode, string CalendarCode, string Direction, string Category, IReadOnlyList<StopTimeInput> Stops) : ICommand<Guid>;

public sealed class AddServiceCommandHandler(
    ITimetableVersionStore store, INetworkUnitOfWork unitOfWork, INetworkAuditLog audit, ICurrentUser user)
    : ICommandHandler<AddServiceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddServiceCommand command, CancellationToken cancellationToken)
    {
        var version = await store.GetAsync(command.VersionId, cancellationToken);
        if (version is null)
            return NetworkErrors.VersionNotFound(command.VersionId);

        if (version.Services.Any(s => string.Equals(s.TrainNumber, command.TrainNumber, StringComparison.OrdinalIgnoreCase)))
            return NetworkErrors.TrainNumberTaken(command.TrainNumber);

        var route = await store.GetRouteByCodeAsync(command.RouteCode.Trim().ToUpperInvariant(), cancellationToken);
        if (route is null)
            return NetworkErrors.RouteNotFound(command.RouteCode);

        var calendar = await store.GetCalendarIdAsync(command.CalendarCode.Trim().ToUpperInvariant(), cancellationToken);
        if (calendar is null)
            return NetworkErrors.CalendarNotFound(command.CalendarCode);

        if (!Enum.TryParse<ServiceDirection>(command.Direction, true, out var direction) || !Enum.TryParse<ServiceCategory>(command.Category, true, out var category))
            return Error.Validation("Network.ServiceInvalid", "Direction is Forward or Reverse; category is Regional, Intercity or HighSpeed.");

        var ids = await store.GetStationIdsAsync(command.Stops.Select(s => s.StationCode.Trim().ToUpperInvariant()), cancellationToken);
        var routeStops = route.Stops.ToDictionary(s => s.StationId);
        var drafts = new List<ServiceStopDraft>();
        foreach (var (stop, index) in command.Stops.Select((s, i) => (s, i)))
        {
            if (!ids.TryGetValue(stop.StationCode.Trim().ToUpperInvariant(), out var stationId))
                return NetworkErrors.StationNotFound(stop.StationCode);
            if (!ServiceTimesCommandHelper.TryParse(stop, out var arrival, out var departure))
                return NetworkErrors.ServiceTimesInvalid;

            var kind = routeStops.TryGetValue(stationId, out var rs) ? rs.Kind : StopKind.Stop;
            drafts.Add(new ServiceStopDraft(index + 1, stationId, arrival, departure, routeStops.GetValueOrDefault(stationId)?.DefaultPlatformId, kind));
        }

        var service = Service.Create(version.Id, route, calendar.Value, command.TrainNumber.Trim(), direction, category, drafts);
        if (service.IsFailure)
            return service.Error;

        var added = version.AddService(service.Value);
        if (added.IsFailure)
            return added.Error;

        store.AddService(service.Value);
        audit.Record(new AuditRecord(user.UserId ?? "unknown", user.Roles.FirstOrDefault(), "timetable.service.add", "timetable_version", version.Id.ToString(), null, null,
            new { command.TrainNumber, command.RouteCode, command.CalendarCode }));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return service.Value.Id;
    }
}

internal static class ServiceTimesCommandHelper
{
    public static bool TryParse(StopTimeInput stop, out int? arrival, out int? departure)
    {
        arrival = null;
        departure = null;
        return ChangeServiceTimesCommandHandler.TryMinutes(stop.Arrival, out arrival) && ChangeServiceTimesCommandHandler.TryMinutes(stop.Departure, out departure);
    }
}

public sealed record RemoveServiceCommand(Guid VersionId, Guid ServiceId) : ICommand;

public sealed class RemoveServiceCommandHandler(
    ITimetableVersionStore store, INetworkUnitOfWork unitOfWork, INetworkAuditLog audit, ICurrentUser user)
    : ICommandHandler<RemoveServiceCommand>
{
    public async Task<Result> Handle(RemoveServiceCommand command, CancellationToken cancellationToken)
    {
        var version = await store.GetAsync(command.VersionId, cancellationToken);
        if (version is null)
            return NetworkErrors.VersionNotFound(command.VersionId);

        var service = version.Services.FirstOrDefault(s => s.Id == command.ServiceId);
        var removed = version.RemoveService(command.ServiceId);
        if (removed.IsFailure)
            return removed;

        store.RemoveService(service!);
        audit.Record(new AuditRecord(user.UserId ?? "unknown", user.Roles.FirstOrDefault(), "timetable.service.remove", "timetable_version", version.Id.ToString(), null,
            new { service!.TrainNumber }, null));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record SubmitTimetableVersionCommand(Guid Id) : ICommand;

public sealed class SubmitTimetableVersionCommandHandler(
    ITimetableVersionStore store, INetworkUnitOfWork unitOfWork, INetworkAuditLog audit, ICurrentUser user)
    : ICommandHandler<SubmitTimetableVersionCommand>
{
    public async Task<Result> Handle(SubmitTimetableVersionCommand command, CancellationToken cancellationToken)
    {
        var version = await store.GetAsync(command.Id, cancellationToken);
        if (version is null)
            return NetworkErrors.VersionNotFound(command.Id);

        var submitted = version.SubmitForReview();
        if (submitted.IsFailure)
            return submitted;

        audit.Record(new AuditRecord(user.UserId ?? "unknown", user.Roles.FirstOrDefault(), "timetable.submit", "timetable_version", version.Id.ToString(), null, null,
            new { version.Label, Services = version.Services.Count }));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// A different person approves and picks the date it takes effect. The version in force is superseded but kept: it still answers for
// the dates before the new one.
public sealed record ApproveTimetableVersionCommand(Guid Id, DateOnly EffectiveFrom) : ICommand;

public sealed class ApproveTimetableVersionCommandHandler(
    ITimetableVersionStore store, INetworkUnitOfWork unitOfWork, INetworkAuditLog audit, INetworkTimeZone timeZone, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<ApproveTimetableVersionCommand>
{
    public async Task<Result> Handle(ApproveTimetableVersionCommand command, CancellationToken cancellationToken)
    {
        var me = user.UserId ?? "unknown";

        var version = await store.GetAsync(command.Id, cancellationToken);
        if (version is null)
            return NetworkErrors.VersionNotFound(command.Id);

        if (command.EffectiveFrom <= timeZone.Today())
            return NetworkErrors.EffectiveDateInPast;
        if (await store.EffectiveDateTakenAsync(command.EffectiveFrom, cancellationToken))
            return NetworkErrors.EffectiveDateTaken(command.EffectiveFrom);

        var published = version.Publish(command.EffectiveFrom, me, clock.GetUtcNow());
        if (published.IsFailure)
            return published;

        foreach (var old in await store.GetPublishedBeforeAsync(command.EffectiveFrom, cancellationToken))
            old.Supersede();

        audit.Record(new AuditRecord(me, user.Roles.FirstOrDefault(), "timetable.publish", "timetable_version", version.Id.ToString(), null, null,
            new { version.Label, command.EffectiveFrom, Author = version.Author }));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
