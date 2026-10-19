namespace SmartRail.Modules.Identity.Contracts;

// Display names for staff accounts (drivers, controllers), so other modules can show a person, not a subject id.
// A subject with no known account is simply absent from the result.
public interface IStaffNames
{
    Task<IReadOnlyDictionary<string, string>> GetNamesAsync(IEnumerable<string> subjects, CancellationToken cancellationToken);
}

// How the display and gate devices are doing, for the dashboards.
public interface IDeviceHealth
{
    Task<DeviceHealthSummary> GetSummaryAsync(CancellationToken cancellationToken);

    // Platform displays and concourse boards at these stations (how many screens a change reaches).
    Task<int> CountDisplaysAtAsync(IReadOnlyCollection<string> stationCodes, CancellationToken cancellationToken);

    // Devices that are Late or Offline, worst first.
    Task<IReadOnlyList<DeviceAttention>> GetAttentionAsync(CancellationToken cancellationToken);
}

public sealed record DeviceHealthSummary(int Total, int Online, int Late, int Offline, int Revoked, int Pending, IReadOnlyList<DeviceTypeCount> ByType);

public sealed record DeviceTypeCount(string Type, int Total, int Online);

public sealed record DeviceAttention(Guid Id, string Name, string Type, string StationCode, string Health, DateTimeOffset? LastHeartbeatUtc);
