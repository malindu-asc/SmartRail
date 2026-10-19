using SmartRail.Application.Core.Abstractions;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Identity.Application.Abstractions;
using SmartRail.Modules.Identity.Domain.Devices;
using SmartRail.Modules.Identity.Domain.Settings;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Identity.Application.Admin;

// ---- Security page ----

public sealed record GetSecuritySettingsQuery : IQuery<SecuritySettingsView>;

public sealed record SecuritySettingsView(SecuritySettings Settings, DateOnly? LastAccessReview, DateOnly? NextAccessReview);

internal static class AccessReviews
{
    public static DateOnly? Next(DateOnly? last, SecuritySettings settings) => last?.AddDays(settings.AccessReviewDays);
}

public sealed class GetSecuritySettingsQueryHandler(ISecuritySettingsStore store) : IQueryHandler<GetSecuritySettingsQuery, SecuritySettingsView>
{
    public async Task<Result<SecuritySettingsView>> Handle(GetSecuritySettingsQuery query, CancellationToken cancellationToken)
    {
        var settings = await store.GetAsync(cancellationToken);
        var last = await store.GetLastAccessReviewAsync(cancellationToken);
        return new SecuritySettingsView(settings, last, AccessReviews.Next(last, settings));
    }
}

public sealed record UpdateSecuritySettingsCommand(SecuritySettings Settings) : ICommand<SecuritySettingsView>;

public sealed class UpdateSecuritySettingsCommandHandler(
    ISecuritySettingsStore store, IStaffDirectory directory, IIdentityAuditLog audit, IIdentityUnitOfWork unitOfWork, ICurrentUser user)
    : ICommandHandler<UpdateSecuritySettingsCommand, SecuritySettingsView>
{
    public async Task<Result<SecuritySettingsView>> Handle(UpdateSecuritySettingsCommand command, CancellationToken cancellationToken)
    {
        var valid = command.Settings.Validate();
        if (valid.IsFailure)
            return valid.Error;

        var before = await store.GetAsync(cancellationToken);
        var actor = user.UserId ?? "unknown";

        // The account-lock numbers live in the sign-in service, so they are applied there first; if that fails nothing is saved.
        if (before.LockAfterFailures != command.Settings.LockAfterFailures || before.LockMinutes != command.Settings.LockMinutes)
        {
            var applied = await directory.ApplyLockoutAsync(command.Settings.LockAfterFailures, command.Settings.LockMinutes, cancellationToken);
            if (applied.IsFailure)
                return applied.Error;
        }

        await store.SaveAsync(command.Settings, actor, cancellationToken);
        audit.Record(new AuditRecord(actor, user.Roles.FirstOrDefault(), "security.settings.update", "security", "settings", null,
            Before: before, After: command.Settings));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var last = await store.GetLastAccessReviewAsync(cancellationToken);
        return new SecuritySettingsView(command.Settings, last, AccessReviews.Next(last, command.Settings));
    }
}

// The reviewers confirmed every account. The countdown on the dashboard starts again.
public sealed record CompleteAccessReviewCommand : ICommand<SecuritySettingsView>;

public sealed class CompleteAccessReviewCommandHandler(
    ISecuritySettingsStore store, IIdentityAuditLog audit, IIdentityUnitOfWork unitOfWork, ICurrentUser user, INetworkTimeZone timeZone)
    : ICommandHandler<CompleteAccessReviewCommand, SecuritySettingsView>
{
    public async Task<Result<SecuritySettingsView>> Handle(CompleteAccessReviewCommand command, CancellationToken cancellationToken)
    {
        var today = timeZone.Today();
        var actor = user.UserId ?? "unknown";
        await store.SaveLastAccessReviewAsync(today, actor, cancellationToken);
        audit.Record(new AuditRecord(actor, user.Roles.FirstOrDefault(), "access_review.complete", "security", "access-review", null,
            After: new { Date = today.ToString("yyyy-MM-dd") }));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var settings = await store.GetAsync(cancellationToken);
        return new SecuritySettingsView(settings, today, AccessReviews.Next(today, settings));
    }
}

// ---- Admin dashboard ----

public sealed record GetAdminDashboardQuery : IQuery<AdminDashboard>;

public sealed record AdminDashboard(
    int StaffAccounts,
    int RolesInUse,
    IReadOnlyDictionary<string, int> StaffByRole,
    int SignedInNow,
    AdminDeviceSummary Devices,
    int FailedSignIns24h,
    int LockedAccounts,
    AccessReviewSummary AccessReview,
    IReadOnlyList<AdminAttentionItem> NeedsAttention,
    IReadOnlyList<AccessChange> RecentAccessChanges);

public sealed record AdminDeviceSummary(int Total, int Online, int NeedAttention);

public sealed record AccessReviewSummary(int? DaysLeft, DateOnly? NextReview, DateOnly? LastReview, int IntervalDays);

// Kind: LeaverStillActive, AccountLocked, DeviceOffline, DeviceLate. Action tells the screen which button to show.
public sealed record AdminAttentionItem(string Kind, string Title, string Detail, string TargetType, string TargetId, string Action);

public sealed record AccessChange(DateTimeOffset OccurredAt, string Text, string? ActorName);

public sealed class GetAdminDashboardQueryHandler(
    IStaffDirectory directory,
    IDeviceStore devices,
    ISecuritySettingsStore settingsStore,
    IEnumerable<IAuditSource> auditSources,
    INetworkTimeZone timeZone,
    TimeProvider clock)
    : IQueryHandler<GetAdminDashboardQuery, AdminDashboard>
{
    public async Task<Result<AdminDashboard>> Handle(GetAdminDashboardQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var settings = await settingsStore.GetAsync(cancellationToken);
        var stats = await directory.GetStatsAsync(cancellationToken);
        if (stats.IsFailure)
            return stats.Error;

        var all = await devices.ListAsync(cancellationToken);
        var late = TimeSpan.FromSeconds(settings.DeviceLateAfterSeconds);
        var offline = TimeSpan.FromSeconds(settings.DeviceOfflineAfterSeconds);
        var health = all.Select(d => (Device: d, Health: d.HealthAt(now, late, offline))).ToList();
        var live = health.Where(h => h.Health != DeviceHealth.Revoked).ToList();
        var online = live.Count(h => h.Health == DeviceHealth.Online);
        var attention = live.Where(h => h.Health is DeviceHealth.Offline or DeviceHealth.Late).ToList();

        var items = new List<AdminAttentionItem>();
        var people = await directory.ListAsync(new StaffFilter(null, null, null, null, 0, 200), cancellationToken);
        if (people.IsSuccess)
        {
            foreach (var p in people.Value.Where(p => p.Status == "Leaver"))
                items.Add(new AdminAttentionItem("LeaverStillActive", "Leaver still has access",
                    $"{p.FullName} · left {p.EmploymentEnd:d MMM}", "user", p.Id, "Disable"));
            foreach (var p in people.Value.Where(p => p.Status == "Locked"))
                items.Add(new AdminAttentionItem("AccountLocked", "Account locked", $"{p.FullName} · {p.FailedSignIns} failed sign-ins", "user", p.Id, "Review"));
        }

        foreach (var (device, h) in attention.OrderByDescending(a => a.Health == DeviceHealth.Offline))
        {
            var age = device.LastHeartbeatAt is null ? "never" : Age(now - device.LastHeartbeatAt.Value);
            items.Add(h == DeviceHealth.Offline
                ? new AdminAttentionItem("DeviceOffline", "Display offline", $"{device.Name} · {age}", "device", device.Id.ToString(), "Open")
                : new AdminAttentionItem("DeviceLate", "Device late", $"{device.Name} · {age}", "device", device.Id.ToString(), "Open"));
        }

        var changes = new List<AccessChange>();
        foreach (var source in auditSources.Where(s => s.IsAccess))
        {
            var rows = await source.QueryAsync(new AuditFilter(null, null, null, null, null, 5), cancellationToken);
            var names = await directory.GetNamesAsync(rows.SelectMany(r => new[] { r.ActorSubject, r.TargetId ?? string.Empty }).Where(s => s.Length > 0).Distinct(), cancellationToken);
            changes.AddRange(rows.Select(r => new AccessChange(r.OccurredAt, Describe(r, names), names.GetValueOrDefault(r.ActorSubject))));
        }

        var last = await settingsStore.GetLastAccessReviewAsync(cancellationToken);
        var next = AccessReviews.Next(last, settings);
        int? daysLeft = next is null ? null : next.Value.DayNumber - timeZone.Today().DayNumber;

        return new AdminDashboard(
            stats.Value.Total,
            stats.Value.ByRole.Count(r => r.Value > 0),
            stats.Value.ByRole,
            stats.Value.SignedInNow,
            new AdminDeviceSummary(live.Count, online, attention.Count),
            stats.Value.FailedSignIns24h,
            stats.Value.LockedAccounts,
            new AccessReviewSummary(daysLeft, next, last, settings.AccessReviewDays),
            items,
            changes.OrderByDescending(c => c.OccurredAt).Take(5).ToList());
    }

    private static string Age(TimeSpan age) =>
        age.TotalMinutes >= 2 ? $"{(int)age.TotalMinutes} min" : $"{(int)age.TotalSeconds} s";

    // "Anil Silva given role Driver", "Display Oak Park P1 registered".
    private static string Describe(AuditRow row, IReadOnlyDictionary<string, string> names)
    {
        var target = row.TargetId is not null && names.TryGetValue(row.TargetId, out var n) ? n : row.TargetId ?? row.TargetType;
        return row.Action switch
        {
            "user.role.change" => $"{target} role changed",
            "user.invite" => $"{target} invited",
            "user.disable" => $"{target} disabled",
            "user.enable" => $"{target} enabled",
            "user.unlock" => $"{target} unlocked",
            "device.register" => "Device registered",
            "device.revoke" => "Device revoked",
            "device.pair" => "Device paired",
            "security.settings.update" => "Security settings changed",
            "access_review.complete" => "Access review completed",
            _ => row.Action
        };
    }
}
