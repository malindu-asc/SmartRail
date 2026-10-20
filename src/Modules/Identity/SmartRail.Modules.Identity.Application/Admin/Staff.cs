using FluentValidation;
using SmartRail.Application.Core.Abstractions;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Identity.Application.Abstractions;
using SmartRail.Modules.Identity.Domain.Errors;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Identity.Application.Admin;

// The staff roles an administrator may give (mirrors SmartRail.Api.Core.Security.Roles.Staff; Application does not reference the API layer).
public static class StaffRoles
{
    public const string Driver = "driver";
    public const string Inspector = "inspector";
    public const string Controller = "controller";
    public const string StationMaster = "station_master";
    public const string Scheduler = "scheduler";
    public const string Admin = "admin";

    public static readonly IReadOnlyList<string> All = [Driver, Inspector, Controller, StationMaster, Scheduler, Admin];
}

internal static class StaffAudit
{
    public static AuditRecord Record(ICurrentUser user, string action, string targetId, string? reason, object? before = null, object? after = null) =>
        new(user.UserId ?? "unknown", user.Roles.FirstOrDefault(), action, "user", targetId, reason, before, after);
}

// ---- Users page ----

public sealed record ListStaffQuery(string? Role, string? Status, string? Search, string? StationCode, int Skip, int Take)
    : IQuery<StaffPage>;

public sealed record StaffPage(IReadOnlyList<StaffAccount> Items, int Skip, int Take);

public sealed class ListStaffQueryHandler(IStaffDirectory directory) : IQueryHandler<ListStaffQuery, StaffPage>
{
    public async Task<Result<StaffPage>> Handle(ListStaffQuery query, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(query.Take <= 0 ? 50 : query.Take, 1, 200);
        var skip = Math.Max(query.Skip, 0);

        if (!string.IsNullOrWhiteSpace(query.Role) && !StaffRoles.All.Contains(query.Role))
            return IdentityErrors.NotAStaffRole;

        var list = await directory.ListAsync(new StaffFilter(query.Role, query.Status, query.Search, query.StationCode, skip, take), cancellationToken);
        return list.IsSuccess ? new StaffPage(list.Value, skip, take) : list.Error;
    }
}

public sealed record GetStaffQuery(string Id) : IQuery<StaffAccount>;

public sealed class GetStaffQueryHandler(IStaffDirectory directory) : IQueryHandler<GetStaffQuery, StaffAccount>
{
    public Task<Result<StaffAccount>> Handle(GetStaffQuery query, CancellationToken cancellationToken) =>
        directory.GetAsync(query.Id, cancellationToken);
}

// ---- Invite ----

public sealed record InviteStaffCommand(string FullName, string Email, string Role, IReadOnlyList<string> StationCodes) : ICommand<StaffInvite>;

public sealed class InviteStaffCommandValidator : AbstractValidator<InviteStaffCommand>
{
    public InviteStaffCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(200).EmailAddress();
    }
}

public sealed class InviteStaffCommandHandler(
    IStaffDirectory directory, INetworkStations stations, IIdentityAuditLog audit, IIdentityUnitOfWork unitOfWork, ICurrentUser user)
    : ICommandHandler<InviteStaffCommand, StaffInvite>
{
    public async Task<Result<StaffInvite>> Handle(InviteStaffCommand command, CancellationToken cancellationToken)
    {
        if (!StaffRoles.All.Contains(command.Role))
            return IdentityErrors.NotAStaffRole;

        var codes = command.StationCodes.Select(c => c.Trim().ToUpperInvariant()).Where(c => c.Length > 0).Distinct().ToList();
        if (command.Role == StaffRoles.StationMaster)
        {
            if (codes.Count == 0)
                return IdentityErrors.StationRequired;

            var known = await stations.GetNamesAsync(codes, null, cancellationToken);
            if (codes.Any(c => !known.ContainsKey(c)))
                return IdentityErrors.DeviceStationInvalid;
        }

        var invited = await directory.InviteAsync(
            new InviteStaff(command.FullName.Trim(), command.Email.Trim().ToLowerInvariant(), command.Role, codes), cancellationToken);
        if (invited.IsFailure)
            return invited.Error;

        audit.Record(StaffAudit.Record(user, "user.invite", invited.Value.Account.Id, null,
            after: new { invited.Value.Account.Email, command.Role, StationCodes = codes }));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return invited;
    }
}

// ---- Edit (name, email, role, stations) ----

public sealed record UpdateStaffCommand(string Id, string? FullName, string? Email, string? Role, IReadOnlyList<string>? StationCodes, string? Reason)
    : ICommand<StaffAccount>;

public sealed class UpdateStaffCommandHandler(
    IStaffDirectory directory, INetworkStations stations, IIdentityAuditLog audit, IIdentityUnitOfWork unitOfWork, ICurrentUser user)
    : ICommandHandler<UpdateStaffCommand, StaffAccount>
{
    public async Task<Result<StaffAccount>> Handle(UpdateStaffCommand command, CancellationToken cancellationToken)
    {
        var current = await directory.GetAsync(command.Id, cancellationToken);
        if (current.IsFailure)
            return current.Error;

        var account = current.Value;
        var roleChange = command.Role is not null && !account.Roles.SequenceEqual([command.Role]);

        if (roleChange)
        {
            if (!StaffRoles.All.Contains(command.Role!))
                return IdentityErrors.NotAStaffRole;
            if (string.Equals(command.Id, user.UserId, StringComparison.Ordinal))
                return IdentityErrors.CannotChangeOwnAccess;
            if (string.IsNullOrWhiteSpace(command.Reason))
                return IdentityErrors.ReasonRequired;

            if (account.Roles.Contains(StaffRoles.Admin) && command.Role != StaffRoles.Admin && await IsLastAdminAsync(cancellationToken))
                return IdentityErrors.LastAdministrator;
        }

        var targetRole = command.Role ?? account.Roles.FirstOrDefault();
        var codes = command.StationCodes?.Select(c => c.Trim().ToUpperInvariant()).Where(c => c.Length > 0).Distinct().ToList();
        if (targetRole == StaffRoles.StationMaster)
        {
            var wanted = codes ?? account.StationCodes.ToList();
            if (wanted.Count == 0)
                return IdentityErrors.StationRequired;

            var known = await stations.GetNamesAsync(wanted, null, cancellationToken);
            if (wanted.Any(c => !known.ContainsKey(c)))
                return IdentityErrors.DeviceStationInvalid;
        }

        var updated = await directory.UpdateAsync(
            command.Id, new UpdateStaff(command.FullName?.Trim(), command.Email?.Trim().ToLowerInvariant(), command.Role, codes), cancellationToken);
        if (updated.IsFailure)
            return updated.Error;

        audit.Record(StaffAudit.Record(user, roleChange ? "user.role.change" : "user.update", command.Id, command.Reason,
            before: new { account.FullName, account.Email, account.Roles, account.StationCodes },
            after: new { updated.Value.FullName, updated.Value.Email, updated.Value.Roles, updated.Value.StationCodes }));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return updated;
    }

    // True when no other enabled administrator exists.
    private async Task<bool> IsLastAdminAsync(CancellationToken cancellationToken)
    {
        var admins = await directory.ListAsync(new StaffFilter(StaffRoles.Admin, "Active", null, null, 0, 50), cancellationToken);
        return admins.IsSuccess && admins.Value.Count <= 1;
    }
}

// ---- Disable and enable (a leaver, or someone back from leave) ----

public sealed record SetStaffEnabledCommand(string Id, bool Enabled, string? Reason) : ICommand;

public sealed class SetStaffEnabledCommandHandler(
    IStaffDirectory directory, IIdentityAuditLog audit, IIdentityUnitOfWork unitOfWork, ICurrentUser user)
    : ICommandHandler<SetStaffEnabledCommand>
{
    public async Task<Result> Handle(SetStaffEnabledCommand command, CancellationToken cancellationToken)
    {
        if (!command.Enabled)
        {
            if (string.Equals(command.Id, user.UserId, StringComparison.Ordinal))
                return IdentityErrors.CannotChangeOwnAccess;
            if (string.IsNullOrWhiteSpace(command.Reason))
                return IdentityErrors.ReasonRequired;
        }

        var current = await directory.GetAsync(command.Id, cancellationToken);
        if (current.IsFailure)
            return current.Error;

        if (!command.Enabled && current.Value.Roles.Contains(StaffRoles.Admin))
        {
            var admins = await directory.ListAsync(new StaffFilter(StaffRoles.Admin, "Active", null, null, 0, 50), cancellationToken);
            if (admins.IsSuccess && admins.Value.Count <= 1)
                return IdentityErrors.LastAdministrator;
        }

        var result = await directory.SetEnabledAsync(command.Id, command.Enabled, cancellationToken);
        if (result.IsFailure)
            return result;

        audit.Record(StaffAudit.Record(user, command.Enabled ? "user.enable" : "user.disable", command.Id, command.Reason,
            before: new { current.Value.Status }, after: new { Status = command.Enabled ? "Active" : "Disabled" }));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ---- Unlock a locked account ----

public sealed record UnlockStaffCommand(string Id, string? Reason) : ICommand;

public sealed class UnlockStaffCommandHandler(IStaffDirectory directory, IIdentityAuditLog audit, IIdentityUnitOfWork unitOfWork, ICurrentUser user)
    : ICommandHandler<UnlockStaffCommand>
{
    public async Task<Result> Handle(UnlockStaffCommand command, CancellationToken cancellationToken)
    {
        var result = await directory.UnlockAsync(command.Id, cancellationToken);
        if (result.IsFailure)
            return result;

        audit.Record(StaffAudit.Record(user, "user.unlock", command.Id, command.Reason));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
