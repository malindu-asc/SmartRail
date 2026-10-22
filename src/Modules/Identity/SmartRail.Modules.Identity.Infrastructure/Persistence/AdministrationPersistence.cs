using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Infrastructure.Core.Audit;
using SmartRail.Modules.Identity.Application.Abstractions;
using SmartRail.Modules.Identity.Application.Admin;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Identity.Domain.Devices;
using SmartRail.Modules.Identity.Domain.Settings;

namespace SmartRail.Modules.Identity.Infrastructure.Persistence;

public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("devices", t =>
        {
            t.HasCheckConstraint("ck_devices_type", "\"Type\" in ('PlatformDisplay','ConcourseBoard','GateScanner','Terminal')");
            t.HasCheckConstraint("ck_devices_status", "\"Status\" in ('Pending','Active','Revoked')");
        });

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Name).IsRequired().HasMaxLength(80);
        builder.Property(d => d.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.StationCode).IsRequired().HasMaxLength(5);
        builder.Property(d => d.Location).HasMaxLength(120);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(d => d.SoftwareVersion).HasMaxLength(30);
        builder.Property(d => d.RegisteredBy).IsRequired().HasMaxLength(64);
        builder.Property(d => d.RevokedBy).HasMaxLength(64);
        builder.Property(d => d.RevokedReason).HasMaxLength(300);

        builder.HasIndex(d => d.StationCode);
    }
}

public sealed class SettingEntryConfiguration : IEntityTypeConfiguration<SettingEntry>
{
    public void Configure(EntityTypeBuilder<SettingEntry> builder)
    {
        builder.ToTable("settings");
        builder.HasKey(s => s.Key);
        builder.Property(s => s.Key).HasMaxLength(60);
        builder.Property(s => s.Value).IsRequired().HasMaxLength(200);
        builder.Property(s => s.UpdatedBy).IsRequired().HasMaxLength(64);
    }
}

internal sealed class DeviceStore(IdentityDbContext context) : IDeviceStore
{
    public Task<Device?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Device>> ListAsync(CancellationToken cancellationToken) =>
        await context.Devices.AsNoTracking().OrderBy(d => d.Name).ToListAsync(cancellationToken);

    public void Add(Device device) => context.Devices.Add(device);
}

internal sealed class SecuritySettingsStore(IdentityDbContext context, TimeProvider clock) : ISecuritySettingsStore
{
    private const string LastReview = "access_review_last";

    public async Task<SecuritySettings> GetAsync(CancellationToken cancellationToken)
    {
        var rows = await context.Settings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);
        var d = SecuritySettings.Default;

        int Int(string key, int fallback) => rows.TryGetValue(key, out var v) && int.TryParse(v, CultureInfo.InvariantCulture, out var n) ? n : fallback;
        bool Bool(string key, bool fallback) => rows.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : fallback;

        return new SecuritySettings(
            Int("lock_after_failures", d.LockAfterFailures), Int("lock_minutes", d.LockMinutes), Bool("two_step_required", d.TwoStepRequired),
            Int("console_idle_minutes", d.ConsoleIdleMinutes), Int("driver_lock_minutes", d.DriverAppLockMinutes),
            Int("access_review_days", d.AccessReviewDays), Bool("flag_leavers", d.FlagLeavers),
            Int("device_late_after_seconds", d.DeviceLateAfterSeconds), Int("device_offline_after_seconds", d.DeviceOfflineAfterSeconds));
    }

    public async Task SaveAsync(SecuritySettings s, string updatedBy, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string>
        {
            ["lock_after_failures"] = s.LockAfterFailures.ToString(CultureInfo.InvariantCulture),
            ["lock_minutes"] = s.LockMinutes.ToString(CultureInfo.InvariantCulture),
            ["two_step_required"] = s.TwoStepRequired.ToString(),
            ["console_idle_minutes"] = s.ConsoleIdleMinutes.ToString(CultureInfo.InvariantCulture),
            ["driver_lock_minutes"] = s.DriverAppLockMinutes.ToString(CultureInfo.InvariantCulture),
            ["access_review_days"] = s.AccessReviewDays.ToString(CultureInfo.InvariantCulture),
            ["flag_leavers"] = s.FlagLeavers.ToString(),
            ["device_late_after_seconds"] = s.DeviceLateAfterSeconds.ToString(CultureInfo.InvariantCulture),
            ["device_offline_after_seconds"] = s.DeviceOfflineAfterSeconds.ToString(CultureInfo.InvariantCulture),
        };

        await UpsertAsync(values, updatedBy, cancellationToken);
    }

    public async Task<DateOnly?> GetLastAccessReviewAsync(CancellationToken cancellationToken)
    {
        var value = await context.Settings.AsNoTracking().Where(s => s.Key == LastReview).Select(s => s.Value).FirstOrDefaultAsync(cancellationToken);
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", out var date) ? date : null;
    }

    public Task SaveLastAccessReviewAsync(DateOnly date, string updatedBy, CancellationToken cancellationToken) =>
        UpsertAsync(new Dictionary<string, string> { [LastReview] = date.ToString("yyyy-MM-dd") }, updatedBy, cancellationToken);

    // Added to the context; the handler's unit of work saves them together with the audit row.
    private async Task UpsertAsync(Dictionary<string, string> values, string updatedBy, CancellationToken cancellationToken)
    {
        var keys = values.Keys.ToList();
        var existing = await context.Settings.Where(s => keys.Contains(s.Key)).ToDictionaryAsync(s => s.Key, cancellationToken);
        var now = clock.GetUtcNow();

        foreach (var (key, value) in values)
        {
            if (existing.TryGetValue(key, out var row))
                row.Change(value, updatedBy, now);
            else
                context.Settings.Add(new SettingEntry(key, value, updatedBy, now));
        }
    }
}

internal sealed class IdentityAuditLog(IdentityDbContext context, TimeProvider clock)
    : AuditLogWriter(context, clock), IIdentityAuditLog;

// What the dashboards and the device sign-in need from the device table.
internal sealed class DeviceDirectory(IdentityDbContext context, ISecuritySettingsStore settings, TimeProvider clock) : IDeviceHealth
{
    public async Task<DeviceHealthSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var (devices, late, offline) = await LoadAsync(cancellationToken);
        var now = clock.GetUtcNow();
        var health = devices.Select(d => (Device: d, Health: d.HealthAt(now, late, offline))).ToList();
        var live = health.Where(h => h.Health != DeviceHealth.Revoked).ToList();

        var byType = live.GroupBy(h => h.Device.Type)
            .Select(g => new DeviceTypeCount(g.Key.ToString(), g.Count(), g.Count(h => h.Health == DeviceHealth.Online)))
            .OrderBy(t => t.Type).ToList();

        return new DeviceHealthSummary(
            live.Count, live.Count(h => h.Health == DeviceHealth.Online), live.Count(h => h.Health == DeviceHealth.Late),
            live.Count(h => h.Health == DeviceHealth.Offline), health.Count(h => h.Health == DeviceHealth.Revoked),
            live.Count(h => h.Health == DeviceHealth.Pending), byType);
    }

    public async Task<int> CountDisplaysAtAsync(IReadOnlyCollection<string> stationCodes, CancellationToken cancellationToken) =>
        await context.Devices.AsNoTracking().CountAsync(d => d.Status != DeviceStatus.Revoked && stationCodes.Contains(d.StationCode)
            && (d.Type == DeviceType.PlatformDisplay || d.Type == DeviceType.ConcourseBoard), cancellationToken);

    public async Task<IReadOnlyList<DeviceAttention>> GetAttentionAsync(CancellationToken cancellationToken)
    {
        var (devices, late, offline) = await LoadAsync(cancellationToken);
        var now = clock.GetUtcNow();

        return devices
            .Select(d => (Device: d, Health: d.HealthAt(now, late, offline)))
            .Where(h => h.Health is DeviceHealth.Offline or DeviceHealth.Late)
            .OrderByDescending(h => h.Health == DeviceHealth.Offline).ThenBy(h => h.Device.LastHeartbeatAt)
            .Select(h => new DeviceAttention(h.Device.Id, h.Device.Name, h.Device.Type.ToString(), h.Device.StationCode, h.Health.ToString(), h.Device.LastHeartbeatAt))
            .ToList();
    }

    private async Task<(List<Device> Devices, TimeSpan Late, TimeSpan Offline)> LoadAsync(CancellationToken cancellationToken)
    {
        var s = await settings.GetAsync(cancellationToken);
        var devices = await context.Devices.AsNoTracking().ToListAsync(cancellationToken);
        return (devices, TimeSpan.FromSeconds(s.DeviceLateAfterSeconds), TimeSpan.FromSeconds(s.DeviceOfflineAfterSeconds));
    }
}
