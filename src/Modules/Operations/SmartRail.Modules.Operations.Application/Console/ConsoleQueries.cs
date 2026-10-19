using System.Text.Json;
using SmartRail.Application.Core.Abstractions;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Journeys.Contracts;
using SmartRail.Modules.Network.Contracts;
using SmartRail.Modules.Operations.Application.Abstractions;
using SmartRail.Modules.Operations.Application.Disruptions;
using SmartRail.Modules.Operations.Application.Messages;
using SmartRail.Modules.Operations.Domain.Disruptions;
using SmartRail.Modules.Ticketing.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Operations.Application.Console;

// ---- Network dashboard (UC-OPS-01) ----

public sealed record DashboardKpis(
    int Running,
    int? OnTimePercent,
    int Delayed,
    int DelayedOver15,
    int Cancelled,
    int NoLiveData,
    int DisruptionsActive,
    int DisruptionsAwaitingApproval);

// Kind: AssistanceRequested, NoLiveData, MessageNotAcknowledged, NoDriver, NoUnit, UnitOutOfService, MajorDelay, Interrupted,
// ApprovalNeeded, DisruptionExpired. Action: Open, Assign or Review. TargetType and TargetId say where Open goes.
public sealed record DashboardAttention(string Kind, string Title, string Detail, string Action, string TargetType, string TargetId, int Severity);

public sealed record DashboardDevices(int Total, int Online, int Late, int Offline, IReadOnlyList<DeviceTypeCount> ByType);

public sealed record DashboardDisruption(Guid Id, string Title, string Severity, DateTimeOffset? SinceUtc, string Scope);

public sealed record OpsDashboard(
    DateTimeOffset AsOfUtc,
    DashboardKpis Kpis,
    IReadOnlyList<DashboardAttention> NeedsAttention,
    DashboardDevices Devices,
    IReadOnlyList<DashboardDisruption> ActiveDisruptions);

public sealed record GetOpsDashboardQuery : IQuery<OpsDashboard>;

public sealed class GetOpsDashboardQueryHandler(
    IOpsJourneyStats journeys, IDisruptionStore disruptions, IMessageStore messages, IAssistanceStore assistance, IStaffNames staff,
    IDeviceHealth devices, INetworkStations stations, INetworkTimeZone timeZone, ICurrentUser user, TimeProvider clock)
    : IQueryHandler<GetOpsDashboardQuery, OpsDashboard>
{
    public async Task<Result<OpsDashboard>> Handle(GetOpsDashboardQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var stats = await journeys.GetAsync(cancellationToken);
        var open = await disruptions.ListOpenAsync(cancellationToken);
        var live = open.Where(d => d.Status == DisruptionStatus.Published).ToList();
        var waiting = open.Where(d => d.Status == DisruptionStatus.AwaitingApproval).ToList();

        var items = new List<DashboardAttention>();

        // A driver asked for help.
        var help = await assistance.ListOpenAsync(cancellationToken);
        var helpNames = await staff.GetNamesAsync(help.Select(h => h.DriverSubject).Distinct(), cancellationToken);
        var places = await stations.GetNamesAsync(help.Select(h => h.StationCode).Where(c => c is not null).Select(c => c!).Distinct(), null, cancellationToken);
        foreach (var h in help)
        {
            var where = h.StationCode is null ? string.Empty : places.GetValueOrDefault(h.StationCode, h.StationCode);
            items.Add(new DashboardAttention("AssistanceRequested", "Assistance requested",
                $"{timeZone.Hm(h.RequestedAtUtc)} {where} · {helpNames.GetValueOrDefault(h.DriverSubject, h.DriverSubject)} · {Ago(now - h.RequestedAtUtc)}".Replace("  ", " "),
                "Open", "assistance", h.Id.ToString(), 0));
        }

        // Journey-level: no live data, no driver or unit, unit out of service, major delay, interrupted.
        foreach (var a in await journeys.GetAttentionAsync(cancellationToken))
            items.Add(new DashboardAttention(a.Kind, a.Title, a.Detail, a.Action, "journey", a.JourneyId.ToString(), a.Severity));

        // A message the driver has not acknowledged.
        var overdue = (await messages.ListSinceAsync(timeZone.ToUtc(timeZone.Today(), 0), cancellationToken))
            .Where(m => m.IsOverdue(now, MessageViews.OverdueAfter)).ToList();
        foreach (var m in overdue)
            items.Add(new DashboardAttention("MessageNotAcknowledged", "Message not acknowledged", $"{(m.UnitCode is null ? "Driver" : $"Unit {m.UnitCode}")} · sent {Ago(now - m.SentAtUtc)}",
                "Open", "message", m.Id.ToString(), 1));

        // A second person is needed for a network-wide notice (not the author, who cannot approve it).
        var authors = await staff.GetNamesAsync(waiting.Select(w => w.CreatedBy).Distinct(), cancellationToken);
        foreach (var w in waiting.Where(w => !string.Equals(w.CreatedBy, user.UserId, StringComparison.Ordinal)))
            items.Add(new DashboardAttention("ApprovalNeeded", "Approval needed", $"Network-wide notice from {authors.GetValueOrDefault(w.CreatedBy, w.CreatedBy)}", "Review", "disruption", w.Id.ToString(), 1));

        foreach (var d in live.Where(d => d.IsExpired(now)))
            items.Add(new DashboardAttention("DisruptionExpired", "Disruption needs review", $"{d.Title} · review time passed", "Review", "disruption", d.Id.ToString(), 2));

        var health = await devices.GetSummaryAsync(cancellationToken);
        var names = await stations.GetNamesAsync(live.SelectMany(d => d.StationCodes).Distinct(), null, cancellationToken);

        return new OpsDashboard(
            now,
            new DashboardKpis(stats.Running, stats.OnTimePercent, stats.Delayed, stats.DelayedOver15, stats.Cancelled, stats.NoLiveData, live.Count, waiting.Count),
            items.OrderBy(i => i.Severity).ThenBy(i => i.Title).ToList(),
            new DashboardDevices(health.Total, health.Online, health.Late, health.Offline, health.ByType),
            live.OrderByDescending(d => d.Severity).ThenByDescending(d => d.UpdatedAtUtc).Take(5)
                .Select(d => new DashboardDisruption(d.Id, d.Title, d.Severity.ToString(), d.PublishedAtUtc,
                    d.NetworkWide ? "All stations" : string.Join(", ", d.StationCodes.Select(c => names.GetValueOrDefault(c, c))))).ToList());
    }

    private static string Ago(TimeSpan age) =>
        age.TotalMinutes < 1 ? "just now" : age.TotalHours >= 2 ? $"{(int)age.TotalHours} h ago" : $"{(int)age.TotalMinutes} min ago";
}

// ---- Who will be affected, before a risky action is confirmed (UC-OPS-02 step 3, CF-02) ----

public sealed record ImpactView(
    Guid JourneyId,
    string TrainNumber,
    string Action,
    int Tickets,
    int Travellers,
    int? Followers,
    int StationDisplays,
    string? DriverName,
    IReadOnlyList<string> Lines);

// action: cancel | terminate_short | platform | interrupt
public sealed record GetImpactQuery(Guid JourneyId, string Action, int? StopSequence) : IQuery<ImpactView>;

public sealed class GetImpactQueryHandler(
    IOpsJourneyStats journeys, ITicketImpact tickets, IDeviceHealth devices, IStaffNames staff, INetworkStations stations)
    : IQueryHandler<GetImpactQuery, ImpactView>
{
    public async Task<Result<ImpactView>> Handle(GetImpactQuery query, CancellationToken cancellationToken)
    {
        var facts = await journeys.GetFactsAsync(query.JourneyId, cancellationToken);
        if (facts is null)
            return Error.NotFound("Operations.JourneyNotFound", "That journey was not found.");

        var action = query.Action.Trim().ToLowerInvariant();
        var count = await tickets.CountAsync(facts.JourneyId, cancellationToken);

        // The stations the action reaches: all of them for a cancellation, the ones after the end point for a short termination,
        // one for a platform change.
        IReadOnlyCollection<string> reached = action switch
        {
            "platform" when query.StopSequence is { } s && s >= 1 && s <= facts.StationCodes.Count => [facts.StationCodes[s - 1]],
            "terminate_short" when query.StopSequence is { } s2 && s2 >= 1 && s2 <= facts.StationCodes.Count => facts.StationCodes.Skip(s2).ToList(),
            _ => facts.StationCodes
        };

        var displays = await devices.CountDisplaysAtAsync(reached, cancellationToken);
        var driver = facts.DriverSubject is null ? null : (await staff.GetNamesAsync([facts.DriverSubject], cancellationToken)).GetValueOrDefault(facts.DriverSubject);
        var lines = new List<string>();

        switch (action)
        {
            case "cancel":
            case "terminate_short":
                if (count.Tickets > 0)
                {
                    lines.Add($"Alert {count.Tickets} ticket {(count.Tickets == 1 ? "holder" : "holders")}");
                    lines.Add($"Mark {count.Tickets} {(count.Tickets == 1 ? "ticket" : "tickets")} as Affected");
                }

                lines.Add($"Update {displays} platform and concourse {(displays == 1 ? "display" : "displays")}");
                break;
            case "platform":
                var place = reached.FirstOrDefault() is { } code ? (await stations.GetNamesAsync([code], null, cancellationToken)).GetValueOrDefault(code, code) : "the station";
                lines.Add($"Show \"changed\" on displays at {place}");
                break;
            default:
                lines.Add($"Update {displays} platform and concourse {(displays == 1 ? "display" : "displays")}");
                break;
        }

        if (driver is not null)
            lines.Add($"Tell the driver, {driver}");

        return new ImpactView(facts.JourneyId, facts.TrainNumber, action, count.Tickets, count.Travellers, null, displays, driver, lines);
    }
}

// ---- Audit log (REQ-OPS-011): one view over every module the caller may read ----

public sealed record AuditEntryView(
    long Id,
    string Module,
    DateTimeOffset OccurredAtUtc,
    string ActorSubject,
    string? ActorName,
    string? ActorRole,
    string Action,
    string ActionLabel,
    string TargetType,
    string? TargetId,
    string? Target,
    string? Reason);

public sealed record ListAuditQuery(
    bool Operational, bool Access, DateTimeOffset? From, DateTimeOffset? To, string? Actor, string? Action, string? Text, int Take)
    : IQuery<IReadOnlyList<AuditEntryView>>;

public sealed class ListAuditQueryHandler(IEnumerable<IAuditSource> sources, IStaffNames staff)
    : IQueryHandler<ListAuditQuery, IReadOnlyList<AuditEntryView>>
{
    private static readonly string[] SummaryKeys = ["Title", "TrainNumber", "Name", "Email", "Code", "StationCode", "DriverSubject"];

    private static readonly Dictionary<string, string> Labels = new()
    {
        ["journey.cancel"] = "Cancelled journey", ["journey.terminate_short"] = "Terminated journey short", ["journey.interrupt"] = "Interrupted journey",
        ["journey.resume"] = "Resumed journey", ["journey.reinstate"] = "Reinstated journey", ["journey.platform.change"] = "Changed platform",
        ["journey.terminate_emergency"] = "Emergency termination", ["journey.assign"] = "Assigned unit or driver", ["journey.unassign"] = "Removed unit or driver",
        ["journey.manual.create"] = "Created special journey", ["duty.create"] = "Rostered a driver", ["unit.out_of_service"] = "Unit out of service",
        ["unit.back_in_service"] = "Unit back in service", ["disruption.draft"] = "Saved draft notice", ["disruption.submit"] = "Submitted notice",
        ["disruption.publish"] = "Published disruption", ["disruption.update"] = "Updated disruption", ["disruption.approve"] = "Approved notice",
        ["disruption.reject"] = "Rejected notice", ["disruption.resolve"] = "Resolved disruption", ["disruption.retract"] = "Retracted disruption",
        ["incident.declare"] = "Declared incident", ["incident.handover"] = "Handed over incident", ["incident.resolve"] = "Resolved incident",
        ["incident.emergency_termination"] = "Emergency termination", ["incident.link_disruption"] = "Linked disruption",
        ["message.send"] = "Messaged a driver", ["message.resend"] = "Resent a message", ["message.acknowledge"] = "Acknowledged a message",
        ["assistance.request"] = "Asked for assistance", ["assistance.handle"] = "Handled assistance request",
        ["timetable.draft"] = "Drafted timetable", ["timetable.submit"] = "Submitted timetable", ["timetable.publish"] = "Published timetable",
        ["user.invite"] = "Invited user", ["user.role.change"] = "Changed role", ["user.update"] = "Updated user", ["user.disable"] = "Disabled access",
        ["user.enable"] = "Enabled access", ["user.unlock"] = "Unlocked account", ["device.register"] = "Registered device", ["device.revoke"] = "Revoked device",
        ["security.settings.update"] = "Changed security settings", ["access_review.complete"] = "Completed access review"
    };

    public async Task<Result<IReadOnlyList<AuditEntryView>>> Handle(ListAuditQuery query, CancellationToken cancellationToken)
    {
        var filter = new AuditFilter(query.From, query.To, query.Actor, query.Action, query.Text, Math.Clamp(query.Take <= 0 ? 100 : query.Take, 1, 500));
        var rows = new List<AuditRow>();

        foreach (var source in sources.Where(s => s.IsAccess ? query.Access : query.Operational))
            rows.AddRange(await source.QueryAsync(filter, cancellationToken));

        var top = rows.OrderByDescending(r => r.OccurredAt).Take(filter.Take).ToList();
        var names = await staff.GetNamesAsync(top.Select(r => r.ActorSubject).Distinct(), cancellationToken);

        return Result.Success<IReadOnlyList<AuditEntryView>>(top.Select(r => new AuditEntryView(
            r.Id, r.Module, r.OccurredAt, r.ActorSubject, names.GetValueOrDefault(r.ActorSubject), r.ActorRole, r.Action,
            Labels.GetValueOrDefault(r.Action, r.Action), r.TargetType, r.TargetId, Summary(r), r.Reason)).ToList());
    }

    // A readable target taken from what the entry recorded ("Signal fault at Mill Lane", "Train 1004").
    private static string? Summary(AuditRow row)
    {
        if (string.IsNullOrWhiteSpace(row.After))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(row.After);
            foreach (var key in SummaryKeys)
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                    return key == "TrainNumber" ? $"Train {v.GetString()}" : v.GetString();
            }
        }
        catch (JsonException)
        {
            // An entry that is not an object has no summary.
        }

        return null;
    }
}
