using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using SmartRail.Modules.Identity.Application.Abstractions;
using SmartRail.Modules.Identity.Application.Admin;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Identity.Domain.Errors;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Identity.Infrastructure.Accounts;

public sealed class StaffDirectoryOptions
{
    public const string Section = "Identity:Staff";

    // Development only: with no email service the invite returns a one-time temporary password instead of sending a link.
    public bool ReturnTemporaryPassword { get; set; }
}

// Staff accounts live in Keycloak (users with a staff realm role). Nothing about people is copied into our database.
internal sealed class KeycloakStaffDirectory(KeycloakAdminClient keycloak, IOptions<StaffDirectoryOptions> options, TimeProvider clock)
    : IStaffDirectory, IStaffNames, IStaffRoster
{
    async Task<IReadOnlyList<StaffPerson>> IStaffRoster.ListActiveAsync(string role, CancellationToken cancellationToken)
    {
        var all = await ListAsync(new StaffFilter(role, "Active", null, null, 0, 500), cancellationToken);
        return all.IsFailure ? [] : all.Value.Select(a => new StaffPerson(a.Id, a.FullName)).OrderBy(p => p.Name).ToList();
    }

    private const string NoLastName = "-";
    private static readonly TimeSpan NameCacheTime = TimeSpan.FromMinutes(10);
    private static readonly ConcurrentDictionary<string, (string Name, DateTimeOffset At)> Names = new();

    public async Task<Result<IReadOnlyList<StaffAccount>>> ListAsync(StaffFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            var roles = filter.Role is null ? StaffRoles.All : [filter.Role];
            var byId = new Dictionary<string, (JsonNode User, List<string> Roles)>();

            foreach (var role in roles)
            {
                var users = await keycloak.GetAsync($"/roles/{role}/users?first=0&max=500&briefRepresentation=false", cancellationToken);
                if (users is not JsonArray array)
                    continue;

                foreach (var user in array)
                {
                    var id = user?["id"]?.GetValue<string>();
                    if (id is null)
                        continue;

                    if (!byId.TryGetValue(id, out var entry))
                        byId[id] = entry = (user!, []);
                    entry.Roles.Add(role);
                }
            }

            // A filter on one role still shows everything else that person holds.
            if (filter.Role is not null)
            {
                foreach (var id in byId.Keys.ToList())
                {
                    var all = await RolesOfAsync(id, cancellationToken);
                    byId[id] = (byId[id].User, all.Count > 0 ? all : byId[id].Roles);
                }
            }

            IEnumerable<(JsonNode User, List<string> Roles)> people = byId.Values;
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                people = people.Where(p => Contains(p.User["firstName"], s) || Contains(p.User["lastName"], s) || Contains(p.User["email"], s) || Contains(p.User["username"], s));
            }

            if (!string.IsNullOrWhiteSpace(filter.StationCode))
                people = people.Where(p => StationCodesOf(p.User).Contains(filter.StationCode.Trim().ToUpperInvariant()));

            var accounts = new List<StaffAccount>();
            using var gate = new SemaphoreSlim(8);
            var tasks = people.Select(async p =>
            {
                await gate.WaitAsync(cancellationToken);
                try { return await ToAccountAsync(p.User, p.Roles, cancellationToken); }
                finally { gate.Release(); }
            }).ToList();
            accounts.AddRange(await Task.WhenAll(tasks));

            IEnumerable<StaffAccount> result = accounts;
            if (!string.IsNullOrWhiteSpace(filter.Status))
                result = result.Where(a => string.Equals(a.Status, filter.Status, StringComparison.OrdinalIgnoreCase));

            var page = result.OrderBy(a => a.FullName, StringComparer.OrdinalIgnoreCase).Skip(filter.Skip).Take(filter.Take).ToList();
            return page;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return IdentityErrors.DirectoryUnavailable;
        }
    }

    public async Task<Result<StaffAccount>> GetAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var user = await keycloak.GetAsync($"/users/{Uri.EscapeDataString(id)}?userProfileMetadata=false", cancellationToken);
            if (user is null)
                return IdentityErrors.StaffNotFound;

            var roles = await RolesOfAsync(id, cancellationToken);
            if (roles.Count == 0)
                return IdentityErrors.StaffNotFound;   // a passenger is not staff

            return await ToAccountAsync(user, roles, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return IdentityErrors.DirectoryUnavailable;
        }
    }

    public async Task<Result<StaffInvite>> InviteAsync(InviteStaff request, CancellationToken cancellationToken)
    {
        try
        {
            var (first, last) = SplitName(request.FullName);
            var temporary = options.Value.ReturnTemporaryPassword ? NewTemporaryPassword() : null;

            var body = new JsonObject
            {
                ["username"] = request.Email,
                ["email"] = request.Email,
                ["firstName"] = first,
                ["lastName"] = last,
                ["enabled"] = true,
                ["emailVerified"] = false,
                ["requiredActions"] = new JsonArray("UPDATE_PASSWORD", "CONFIGURE_TOTP"),
                ["attributes"] = new JsonObject { ["station_codes"] = new JsonArray(request.StationCodes.Select(c => (JsonNode?)c).ToArray()) }
            };
            if (temporary is not null)
                body["credentials"] = new JsonArray(new JsonObject { ["type"] = "password", ["value"] = temporary, ["temporary"] = true });

            using var created = await keycloak.SendAsync(HttpMethod.Post, "/users", body, cancellationToken);
            if (created.StatusCode == HttpStatusCode.Conflict)
                return IdentityErrors.AccountExists;
            if (!created.IsSuccessStatusCode)
                return IdentityErrors.DirectoryUnavailable;

            var id = created.Headers.Location?.Segments.LastOrDefault()?.Trim('/');
            if (string.IsNullOrEmpty(id))
                return IdentityErrors.DirectoryUnavailable;

            var granted = await SetRoleAsync(id, request.Role, cancellationToken);
            if (granted.IsFailure)
                return granted.Error;

            var emailSent = false;
            if (temporary is null)
            {
                using var email = await keycloak.SendAsync(HttpMethod.Put, $"/users/{id}/execute-actions-email", new[] { "UPDATE_PASSWORD", "CONFIGURE_TOTP" }, cancellationToken);
                emailSent = email.IsSuccessStatusCode;
            }

            var account = await GetAsync(id, cancellationToken);
            return account.IsSuccess ? new StaffInvite(account.Value, emailSent, temporary) : account.Error;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return IdentityErrors.DirectoryUnavailable;
        }
    }

    public async Task<Result<StaffAccount>> UpdateAsync(string id, UpdateStaff request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await keycloak.GetAsync($"/users/{Uri.EscapeDataString(id)}", cancellationToken);
            if (user is null)
                return IdentityErrors.StaffNotFound;

            if (request.FullName is not null)
            {
                var (first, last) = SplitName(request.FullName);
                user["firstName"] = first;
                user["lastName"] = last;
            }

            if (request.Email is not null)
            {
                user["email"] = request.Email;
                user["username"] = request.Email;
            }

            if (request.StationCodes is not null)
            {
                var attributes = user["attributes"] as JsonObject ?? new JsonObject();
                attributes["station_codes"] = new JsonArray(request.StationCodes.Select(c => (JsonNode?)c).ToArray());
                user["attributes"] = attributes;
            }

            using var put = await keycloak.SendAsync(HttpMethod.Put, $"/users/{Uri.EscapeDataString(id)}", user, cancellationToken);
            if (put.StatusCode == HttpStatusCode.Conflict)
                return IdentityErrors.AccountExists;
            if (!put.IsSuccessStatusCode)
                return IdentityErrors.DirectoryUnavailable;

            if (request.Role is not null)
            {
                var changed = await SetRoleAsync(id, request.Role, cancellationToken);
                if (changed.IsFailure)
                    return changed.Error;
            }

            Names.TryRemove(id, out _);
            return await GetAsync(id, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return IdentityErrors.DirectoryUnavailable;
        }
    }

    public async Task<Result> SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken)
    {
        try
        {
            var user = await keycloak.GetAsync($"/users/{Uri.EscapeDataString(id)}", cancellationToken);
            if (user is null)
                return IdentityErrors.StaffNotFound;

            user["enabled"] = enabled;
            using var put = await keycloak.SendAsync(HttpMethod.Put, $"/users/{Uri.EscapeDataString(id)}", user, cancellationToken);
            if (!put.IsSuccessStatusCode)
                return IdentityErrors.DirectoryUnavailable;

            // Their sign-in stops at once, not when the token runs out.
            if (!enabled)
            {
                using var logout = await keycloak.SendAsync(HttpMethod.Post, $"/users/{Uri.EscapeDataString(id)}/logout", null, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return IdentityErrors.DirectoryUnavailable;
        }
    }

    public async Task<Result> UnlockAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await keycloak.SendAsync(HttpMethod.Delete, $"/attack-detection/brute-force/users/{Uri.EscapeDataString(id)}", null, cancellationToken);
            return response.StatusCode switch
            {
                HttpStatusCode.NoContent or HttpStatusCode.OK => Result.Success(),
                HttpStatusCode.NotFound => IdentityErrors.StaffNotFound,
                _ => IdentityErrors.DirectoryUnavailable
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return IdentityErrors.DirectoryUnavailable;
        }
    }

    public async Task<Result<StaffStats>> GetStatsAsync(CancellationToken cancellationToken)
    {
        var all = await ListAsync(new StaffFilter(null, null, null, null, 0, 1000), cancellationToken);
        if (all.IsFailure)
            return all.Error;

        var byRole = StaffRoles.All.ToDictionary(r => r, r => all.Value.Count(a => a.Roles.Contains(r)));

        var signedIn = 0;
        var failed = 0;
        try
        {
            if (await keycloak.GetAsync("/client-session-stats", cancellationToken) is JsonArray stats)
                signedIn = stats.Sum(s => int.TryParse(s?["active"]?.ToString(), out var n) && s?["clientId"]?.GetValue<string>() != "security-admin-console" ? n : 0);

            var since = clock.GetUtcNow().AddHours(-24);
            var date = since.UtcDateTime.ToString("yyyy-MM-dd");
            if (await keycloak.GetAsync($"/events?type=LOGIN_ERROR&dateFrom={date}&max=1000", cancellationToken) is JsonArray events)
                failed = events.Count(e => long.TryParse(e?["time"]?.ToString(), out var ms) && DateTimeOffset.FromUnixTimeMilliseconds(ms) >= since);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // The numbers are a convenience; the account list above is the part that must be right.
        }

        return new StaffStats(
            all.Value.Count, byRole, signedIn, failed,
            all.Value.Count(a => a.Status == "Locked"), all.Value.Count(a => a.Status == "Leaver"), all.Value.Count(a => a.Status == "Disabled"));
    }

    public async Task<IReadOnlyDictionary<string, string>> GetNamesAsync(IEnumerable<string> subjects, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>();
        var missing = new List<string>();
        var now = clock.GetUtcNow();

        foreach (var subject in subjects.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct())
        {
            if (Names.TryGetValue(subject, out var cached) && now - cached.At < NameCacheTime)
                result[subject] = cached.Name;
            else
                missing.Add(subject);
        }

        if (missing.Count == 0)
            return result;

        try
        {
            using var gate = new SemaphoreSlim(8);
            var found = await Task.WhenAll(missing.Select(async id =>
            {
                await gate.WaitAsync(cancellationToken);
                try
                {
                    var user = await keycloak.GetAsync($"/users/{Uri.EscapeDataString(id)}", cancellationToken);
                    return (Id: id, Name: user is null ? null : FullNameOf(user));
                }
                finally { gate.Release(); }
            }));

            foreach (var (id, name) in found.Where(f => !string.IsNullOrWhiteSpace(f.Name)))
            {
                Names[id] = (name!, now);
                result[id] = name!;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // A name is a nicety: without it the caller shows the subject.
        }

        return result;
    }

    public async Task<Result> ApplyLockoutAsync(int failures, int lockMinutes, CancellationToken cancellationToken)
    {
        try
        {
            var realm = await keycloak.GetAsync(string.Empty, cancellationToken);
            if (realm is null)
                return IdentityErrors.DirectoryUnavailable;

            realm["bruteForceProtected"] = true;
            realm["failureFactor"] = failures;
            realm["maxFailureWaitSeconds"] = lockMinutes * 60;
            realm["waitIncrementSeconds"] = lockMinutes * 60;
            realm["permanentLockout"] = false;

            using var put = await keycloak.SendAsync(HttpMethod.Put, string.Empty, realm, cancellationToken);
            return put.IsSuccessStatusCode ? Result.Success() : IdentityErrors.DirectoryUnavailable;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return IdentityErrors.DirectoryUnavailable;
        }
    }

    // ---- helpers ----

    private async Task<StaffAccount> ToAccountAsync(JsonNode user, IReadOnlyList<string> roles, CancellationToken cancellationToken)
    {
        var id = user["id"]!.GetValue<string>();
        var enabled = user["enabled"]?.GetValue<bool>() ?? true;

        var failures = 0;
        var locked = false;
        DateTimeOffset? lastSignIn = null;

        var brute = await keycloak.GetAsync($"/attack-detection/brute-force/users/{id}", cancellationToken);
        if (brute is not null)
        {
            failures = int.TryParse(brute["numFailures"]?.ToString(), out var n) ? n : 0;
            locked = brute["disabled"]?.GetValue<bool>() ?? false;
        }

        if (await keycloak.GetAsync($"/events?user={id}&type=LOGIN&max=1", cancellationToken) is JsonArray events && events.Count > 0
            && long.TryParse(events[0]?["time"]?.ToString(), out var ms))
        {
            lastSignIn = DateTimeOffset.FromUnixTimeMilliseconds(ms);
        }

        DateOnly? end = null;
        var endText = (user["attributes"]?["employment_end"] as JsonArray)?.FirstOrDefault()?.GetValue<string>();
        if (DateOnly.TryParse(endText, out var parsed))
            end = parsed;

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var status = !enabled ? "Disabled" : locked ? "Locked" : end is not null && end <= today ? "Leaver" : "Active";

        return new StaffAccount(
            id, user["username"]?.GetValue<string>() ?? string.Empty, FullNameOf(user), user["email"]?.GetValue<string>() ?? string.Empty,
            roles.OrderBy(r => r).ToList(), status, failures, lastSignIn, StationCodesOf(user), end);
    }

    private async Task<List<string>> RolesOfAsync(string id, CancellationToken cancellationToken)
    {
        if (await keycloak.GetAsync($"/users/{Uri.EscapeDataString(id)}/role-mappings/realm", cancellationToken) is not JsonArray array)
            return [];

        return array.Select(r => r?["name"]?.GetValue<string>()).Where(n => n is not null && StaffRoles.All.Contains(n)).Select(n => n!).ToList();
    }

    // One staff role at a time: the old staff roles are removed, the new one is added. Other realm roles are left alone.
    private async Task<Result> SetRoleAsync(string id, string role, CancellationToken cancellationToken)
    {
        var wanted = await keycloak.GetAsync($"/roles/{role}", cancellationToken);
        if (wanted is null)
            return IdentityErrors.NotAStaffRole;

        if (await keycloak.GetAsync($"/users/{Uri.EscapeDataString(id)}/role-mappings/realm", cancellationToken) is JsonArray current)
        {
            var old = new JsonArray(current.Where(r => StaffRoles.All.Contains(r?["name"]?.GetValue<string>() ?? string.Empty) && r?["name"]?.GetValue<string>() != role)
                .Select(r => JsonNode.Parse(r!.ToJsonString())).ToArray());
            if (old.Count > 0)
            {
                using var remove = await keycloak.SendAsync(HttpMethod.Delete, $"/users/{Uri.EscapeDataString(id)}/role-mappings/realm", old, cancellationToken);
                if (!remove.IsSuccessStatusCode)
                    return IdentityErrors.DirectoryUnavailable;
            }
        }

        using var add = await keycloak.SendAsync(HttpMethod.Post, $"/users/{Uri.EscapeDataString(id)}/role-mappings/realm", new JsonArray(wanted), cancellationToken);
        return add.IsSuccessStatusCode ? Result.Success() : IdentityErrors.DirectoryUnavailable;
    }

    private static string FullNameOf(JsonNode user)
    {
        var first = user["firstName"]?.GetValue<string>();
        var last = user["lastName"]?.GetValue<string>();
        var name = string.Join(' ', new[] { first, last == NoLastName ? null : last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return name.Length > 0 ? name : user["username"]?.GetValue<string>() ?? string.Empty;
    }

    private static IReadOnlyList<string> StationCodesOf(JsonNode user) =>
        (user["attributes"]?["station_codes"] as JsonArray)?.Select(c => c?.GetValue<string>()).Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c!.ToUpperInvariant()).ToList() ?? [];

    private static bool Contains(JsonNode? field, string text) =>
        field?.GetValue<string>()?.Contains(text, StringComparison.OrdinalIgnoreCase) == true;

    private static (string First, string Last) SplitName(string fullName)
    {
        var name = fullName.Trim();
        var space = name.LastIndexOf(' ');
        return space < 0 ? (name, NoLastName) : (name[..space].Trim(), name[(space + 1)..].Trim());
    }

    private static string NewTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var chars = Enumerable.Range(0, 12).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray();
        return new string(chars) + "#7";
    }
}
