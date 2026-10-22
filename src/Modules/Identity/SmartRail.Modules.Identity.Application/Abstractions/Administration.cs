using SmartRail.Application.Core.Abstractions;
using SmartRail.Modules.Identity.Domain.Devices;
using SmartRail.Modules.Identity.Domain.Settings;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Identity.Application.Abstractions;

// This module's audit log: user, role, device and security changes (access changes in the console audit view).
public interface IIdentityAuditLog : IAuditLog;

// ---- staff accounts (Keycloak) ----

public sealed record StaffAccount(
    string Id,
    string UserName,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles,
    string Status,
    int FailedSignIns,
    DateTimeOffset? LastSignInUtc,
    IReadOnlyList<string> StationCodes,
    DateOnly? EmploymentEnd);

public sealed record StaffFilter(string? Role, string? Status, string? Search, string? StationCode, int Skip, int Take);

public sealed record InviteStaff(string FullName, string Email, string Role, IReadOnlyList<string> StationCodes);

public sealed record UpdateStaff(string? FullName, string? Email, string? Role, IReadOnlyList<string>? StationCodes);

// What the invite produced. The temporary password is returned only when no email service is configured (development).
public sealed record StaffInvite(StaffAccount Account, bool EmailSent, string? TemporaryPassword);

public sealed record StaffStats(
    int Total,
    IReadOnlyDictionary<string, int> ByRole,
    int SignedInNow,
    int FailedSignIns24h,
    int LockedAccounts,
    int Leavers,
    int Disabled);

// Where staff accounts live. The API never stores passwords or role assignments of its own.
public interface IStaffDirectory
{
    Task<Result<IReadOnlyList<StaffAccount>>> ListAsync(StaffFilter filter, CancellationToken cancellationToken);

    Task<Result<StaffAccount>> GetAsync(string id, CancellationToken cancellationToken);

    Task<Result<StaffInvite>> InviteAsync(InviteStaff request, CancellationToken cancellationToken);

    Task<Result<StaffAccount>> UpdateAsync(string id, UpdateStaff request, CancellationToken cancellationToken);

    // Disabling stops sign-in at once (sessions are ended); the person's records stay.
    Task<Result> SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken);

    Task<Result> UnlockAsync(string id, CancellationToken cancellationToken);

    Task<Result<StaffStats>> GetStatsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, string>> GetNamesAsync(IEnumerable<string> subjects, CancellationToken cancellationToken);

    // Applies the account-lock numbers to the sign-in service.
    Task<Result> ApplyLockoutAsync(int failures, int lockMinutes, CancellationToken cancellationToken);
}

// ---- devices ----

public interface IDeviceStore
{
    Task<Device?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Device>> ListAsync(CancellationToken cancellationToken);

    void Add(Device device);
}

// ---- security settings ----

public interface ISecuritySettingsStore
{
    Task<SecuritySettings> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(SecuritySettings settings, string updatedBy, CancellationToken cancellationToken);

    // When the last access review was completed; null before the first one.
    Task<DateOnly?> GetLastAccessReviewAsync(CancellationToken cancellationToken);

    Task SaveLastAccessReviewAsync(DateOnly date, string updatedBy, CancellationToken cancellationToken);
}
