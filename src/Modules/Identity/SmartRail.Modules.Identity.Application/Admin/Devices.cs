using System.Security.Cryptography;
using System.Text;
using SmartRail.Application.Core.Abstractions;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Identity.Application.Abstractions;
using SmartRail.Modules.Identity.Domain.Devices;
using SmartRail.Modules.Identity.Domain.Errors;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Identity.Application.Admin;

public static class DeviceSecrets
{
    // Letters and digits that cannot be mistaken for each other when read off a screen.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string NewPairingCode() =>
        new(Enumerable.Range(0, 6).Select(_ => Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]).ToArray());

    // Shown as "7K4 - 9QX".
    public static string Format(string code) => $"{code[..3]} - {code[3..]}";

    // The code as typed: any spacing, hyphen or case is accepted.
    public static string Normalise(string typed) =>
        new(typed.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    public static string NewToken() => "srd_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string Hash(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}

public sealed record DeviceView(
    Guid Id,
    string Name,
    string Type,
    string StationCode,
    string StationName,
    string? Location,
    string Health,
    DateTimeOffset? LastHeartbeatUtc,
    int? LastHeartbeatAgeSeconds,
    string? SoftwareVersion,
    string RegisteredBy,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? PairingExpiresAt,
    DateTimeOffset? RevokedAt,
    string? RevokedBy,
    string? RevokedReason);

public sealed record DeviceList(IReadOnlyList<DeviceView> Items, DeviceCounts Counts);

// The tab counts: All, Displays (platform displays and concourse boards), Gates, Terminals.
public sealed record DeviceCounts(int All, int Displays, int Gates, int Terminals);

internal static class DeviceViews
{
    public static async Task<List<DeviceView>> ToViewsAsync(
        IReadOnlyList<Device> devices, INetworkStations stations, Domain.Settings.SecuritySettings settings, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var names = await stations.GetNamesAsync(devices.Select(d => d.StationCode).Distinct(), null, cancellationToken);
        return devices.Select(d => ToView(d, names, settings, now)).ToList();
    }

    public static DeviceView ToView(Device d, IReadOnlyDictionary<string, string> names, Domain.Settings.SecuritySettings settings, DateTimeOffset now)
    {
        var health = d.HealthAt(now, TimeSpan.FromSeconds(settings.DeviceLateAfterSeconds), TimeSpan.FromSeconds(settings.DeviceOfflineAfterSeconds));
        return new DeviceView(
            d.Id, d.Name, d.Type.ToString(), d.StationCode, names.GetValueOrDefault(d.StationCode, d.StationCode), d.Location,
            health.ToString(), d.LastHeartbeatAt, d.LastHeartbeatAt is null ? null : (int)(now - d.LastHeartbeatAt.Value).TotalSeconds,
            d.SoftwareVersion, d.RegisteredBy, d.RegisteredAt,
            d.Status == DeviceStatus.Pending ? d.PairingExpiresAt : null, d.RevokedAt, d.RevokedBy, d.RevokedReason);
    }
}

// ---- Devices page ----

public sealed record ListDevicesQuery(string? Type, string? Health, string? StationCode, string? Search) : IQuery<DeviceList>;

public sealed class ListDevicesQueryHandler(IDeviceStore store, ISecuritySettingsStore settings, INetworkStations stations, TimeProvider clock)
    : IQueryHandler<ListDevicesQuery, DeviceList>
{
    public async Task<Result<DeviceList>> Handle(ListDevicesQuery query, CancellationToken cancellationToken)
    {
        var all = await store.ListAsync(cancellationToken);
        var views = await DeviceViews.ToViewsAsync(all, stations, await settings.GetAsync(cancellationToken), clock.GetUtcNow(), cancellationToken);

        var counts = new DeviceCounts(
            views.Count,
            views.Count(v => v.Type is nameof(DeviceType.PlatformDisplay) or nameof(DeviceType.ConcourseBoard)),
            views.Count(v => v.Type == nameof(DeviceType.GateScanner)),
            views.Count(v => v.Type == nameof(DeviceType.Terminal)));

        IEnumerable<DeviceView> filtered = views;
        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            filtered = query.Type.ToLowerInvariant() switch
            {
                "displays" => filtered.Where(v => v.Type is nameof(DeviceType.PlatformDisplay) or nameof(DeviceType.ConcourseBoard)),
                "gates" => filtered.Where(v => v.Type == nameof(DeviceType.GateScanner)),
                "terminals" => filtered.Where(v => v.Type == nameof(DeviceType.Terminal)),
                _ => filtered.Where(v => string.Equals(v.Type, query.Type, StringComparison.OrdinalIgnoreCase))
            };
        }

        if (!string.IsNullOrWhiteSpace(query.Health))
            filtered = filtered.Where(v => string.Equals(v.Health, query.Health, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.StationCode))
            filtered = filtered.Where(v => string.Equals(v.StationCode, query.StationCode, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.Search))
            filtered = filtered.Where(v => v.Name.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase)
                || v.StationName.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase));

        // Worst first: offline, late, pending, online, revoked last.
        var order = new[] { "Offline", "Late", "Pending", "Online", "Revoked" };
        var items = filtered.OrderBy(v => Array.IndexOf(order, v.Health)).ThenBy(v => v.Name).ToList();
        return new DeviceList(items, counts);
    }
}

public sealed record GetDeviceQuery(Guid Id) : IQuery<DeviceView>;

public sealed class GetDeviceQueryHandler(IDeviceStore store, ISecuritySettingsStore settings, INetworkStations stations, TimeProvider clock)
    : IQueryHandler<GetDeviceQuery, DeviceView>
{
    public async Task<Result<DeviceView>> Handle(GetDeviceQuery query, CancellationToken cancellationToken)
    {
        var device = await store.GetAsync(query.Id, cancellationToken);
        if (device is null)
            return IdentityErrors.DeviceNotFound;

        var names = await stations.GetNamesAsync([device.StationCode], null, cancellationToken);
        return DeviceViews.ToView(device, names, await settings.GetAsync(cancellationToken), clock.GetUtcNow());
    }
}

// ---- Register ----

public sealed record RegisterDeviceCommand(string Name, string Type, string StationCode, string? Location) : ICommand<RegisteredDevice>;

// The pairing code is shown once. Only its hash is kept.
public sealed record RegisteredDevice(Guid Id, string PairingCode, DateTimeOffset ExpiresAt);

public sealed class RegisterDeviceCommandHandler(
    IDeviceStore store, ISecuritySettingsStore settings, INetworkStations stations, IIdentityAuditLog audit,
    IIdentityUnitOfWork unitOfWork, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<RegisterDeviceCommand, RegisteredDevice>
{
    public async Task<Result<RegisteredDevice>> Handle(RegisterDeviceCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<DeviceType>(command.Type, true, out var type))
            return IdentityErrors.DeviceTypeInvalid;

        var code = command.StationCode.Trim().ToUpperInvariant();
        var known = await stations.GetNamesAsync([code], null, cancellationToken);
        if (!known.ContainsKey(code))
            return IdentityErrors.DeviceStationInvalid;

        var now = clock.GetUtcNow();
        var pairing = DeviceSecrets.NewPairingCode();
        var expires = now.AddMinutes((await settings.GetAsync(cancellationToken)).PairingCodeMinutes);

        var device = Device.Register(command.Name, type, code, command.Location, user.UserId ?? "unknown", DeviceSecrets.Hash(pairing), expires, now);
        if (device.IsFailure)
            return device.Error;

        store.Add(device.Value);
        audit.Record(new AuditRecord(user.UserId ?? "unknown", user.Roles.FirstOrDefault(), "device.register", "device", device.Value.Id.ToString(), null,
            After: new { device.Value.Name, Type = type.ToString(), StationCode = code, device.Value.Location }));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RegisteredDevice(device.Value.Id, DeviceSecrets.Format(pairing), expires);
    }
}

// A fresh code for a device that has not been paired yet.
public sealed record NewDevicePairingCodeCommand(Guid Id) : ICommand<RegisteredDevice>;

public sealed class NewDevicePairingCodeCommandHandler(
    IDeviceStore store, ISecuritySettingsStore settings, IIdentityAuditLog audit, IIdentityUnitOfWork unitOfWork, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<NewDevicePairingCodeCommand, RegisteredDevice>
{
    public async Task<Result<RegisteredDevice>> Handle(NewDevicePairingCodeCommand command, CancellationToken cancellationToken)
    {
        var device = await store.GetAsync(command.Id, cancellationToken);
        if (device is null)
            return IdentityErrors.DeviceNotFound;

        var pairing = DeviceSecrets.NewPairingCode();
        var expires = clock.GetUtcNow().AddMinutes((await settings.GetAsync(cancellationToken)).PairingCodeMinutes);
        var result = device.NewPairingCode(DeviceSecrets.Hash(pairing), expires);
        if (result.IsFailure)
            return result.Error;

        audit.Record(new AuditRecord(user.UserId ?? "unknown", user.Roles.FirstOrDefault(), "device.pairing_code.new", "device", device.Id.ToString(), null));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RegisteredDevice(device.Id, DeviceSecrets.Format(pairing), expires);
    }
}

// ---- Revoke ----

public sealed record RevokeDeviceCommand(Guid Id, string Reason) : ICommand;

public sealed class RevokeDeviceCommandHandler(IDeviceStore store, IIdentityAuditLog audit, IIdentityUnitOfWork unitOfWork, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<RevokeDeviceCommand>
{
    public async Task<Result> Handle(RevokeDeviceCommand command, CancellationToken cancellationToken)
    {
        var device = await store.GetAsync(command.Id, cancellationToken);
        if (device is null)
            return IdentityErrors.DeviceNotFound;

        var result = device.Revoke(user.UserId ?? "unknown", command.Reason, clock.GetUtcNow());
        if (result.IsFailure)
            return result;

        audit.Record(new AuditRecord(user.UserId ?? "unknown", user.Roles.FirstOrDefault(), "device.revoke", "device", device.Id.ToString(), command.Reason,
            After: new { device.Name, device.StationCode }));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
