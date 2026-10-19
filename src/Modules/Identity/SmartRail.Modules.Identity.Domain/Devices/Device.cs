using SmartRail.Modules.Identity.Domain.Errors;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Identity.Domain.Devices;

public enum DeviceType
{
    PlatformDisplay = 0,
    ConcourseBoard = 1,
    GateScanner = 2,
    Terminal = 3
}

// Stored state. What the console shows (Online, Late, Offline) is derived from the last heartbeat (state-models.md section 5).
public enum DeviceStatus
{
    Pending = 0,
    Active = 1,
    Revoked = 2
}

public enum DeviceHealth
{
    Pending = 0,
    Online = 1,
    Late = 2,
    Offline = 3,
    Revoked = 4
}

// A display, gate scanner or terminal. It signs in with its own token, not a person's (REQ-OPS-009). The pairing code and the
// device token are shown once; only their hashes are stored.
public sealed class Device : Entity<Guid>
{
    private Device() { }

    private Device(Guid id) : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public DeviceType Type { get; private set; }
    public string StationCode { get; private set; } = string.Empty;
    public string? Location { get; private set; }
    public DeviceStatus Status { get; private set; }
    public string? PairingCodeHash { get; private set; }
    public DateTimeOffset? PairingExpiresAt { get; private set; }
    public string? SoftwareVersion { get; private set; }
    public string RegisteredBy { get; private set; } = string.Empty;
    public DateTimeOffset RegisteredAt { get; private set; }
    public DateTimeOffset? LastHeartbeatAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedBy { get; private set; }
    public string? RevokedReason { get; private set; }

    public static Result<Device> Register(
        string name, DeviceType type, string stationCode, string? location, string registeredBy,
        string pairingCodeHash, DateTimeOffset pairingExpiresAt, DateTimeOffset now, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80)
            return IdentityErrors.DeviceNameInvalid;
        if (string.IsNullOrWhiteSpace(stationCode) || stationCode.Trim().Length > 5)
            return IdentityErrors.DeviceStationInvalid;

        return new Device(id ?? Guid.NewGuid())
        {
            Name = name.Trim(),
            Type = type,
            StationCode = stationCode.Trim().ToUpperInvariant(),
            Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim(),
            Status = DeviceStatus.Pending,
            PairingCodeHash = pairingCodeHash,
            PairingExpiresAt = pairingExpiresAt,
            RegisteredBy = registeredBy,
            RegisteredAt = now
        };
    }

    // A new code for a device that has not been paired yet (the first one ran out).
    public Result NewPairingCode(string pairingCodeHash, DateTimeOffset expiresAt)
    {
        if (Status != DeviceStatus.Pending)
            return IdentityErrors.DeviceNotPending;

        PairingCodeHash = pairingCodeHash;
        PairingExpiresAt = expiresAt;
        return Result.Success();
    }

    // Recorded by whatever watches the device (the simulator, or the display service in a later release). The first one activates it.
    public void Heartbeat(DateTimeOffset now, string? softwareVersion)
    {
        if (Status == DeviceStatus.Revoked)
            return;

        Status = DeviceStatus.Active;
        PairingCodeHash = null;
        PairingExpiresAt = null;
        LastHeartbeatAt = now;
        if (!string.IsNullOrWhiteSpace(softwareVersion))
            SoftwareVersion = softwareVersion.Trim();
    }

    // The next request with its token is refused. The record stays for the audit trail.
    public Result Revoke(string revokedBy, string reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return IdentityErrors.ReasonRequired;
        if (Status == DeviceStatus.Revoked)
            return IdentityErrors.DeviceAlreadyRevoked;

        Status = DeviceStatus.Revoked;
        PairingCodeHash = null;
        PairingExpiresAt = null;
        RevokedAt = now;
        RevokedBy = revokedBy;
        RevokedReason = reason.Trim();
        return Result.Success();
    }

    // Heartbeat every 30 s; late after 60 s; offline after 90 s (both limits are settings).
    public DeviceHealth HealthAt(DateTimeOffset now, TimeSpan lateAfter, TimeSpan offlineAfter)
    {
        if (Status == DeviceStatus.Revoked)
            return DeviceHealth.Revoked;
        if (Status == DeviceStatus.Pending || LastHeartbeatAt is null)
            return DeviceHealth.Pending;

        var age = now - LastHeartbeatAt.Value;
        return age >= offlineAfter ? DeviceHealth.Offline : age >= lateAfter ? DeviceHealth.Late : DeviceHealth.Online;
    }
}
