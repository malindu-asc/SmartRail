using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Api.IntegrationTests.Network;
using SmartRail.Modules.Identity.Application.Abstractions;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Api.IntegrationTests.Ops;

// Staff accounts live in the identity provider; the tests keep them in memory so the administrator console can be exercised on its own.
internal sealed class InMemoryStaffDirectory : IStaffDirectory, IStaffNames
{
    private readonly ConcurrentDictionary<string, StaffAccount> _accounts = new();

    public InMemoryStaffDirectory()
    {
        Add("staff-1", "Nimal Perera", "controller", "ctrl.one@example.test");
        Add("staff-2", "Kasun Silva", "scheduler", "sched.one@example.test");
    }

    private void Add(string id, string name, string role, string email) =>
        _accounts[id] = new StaffAccount(id, email, name, email, [role], "Active", 0, null, [], null);

    public Task<Result<IReadOnlyList<StaffAccount>>> ListAsync(StaffFilter filter, CancellationToken cancellationToken)
    {
        var rows = _accounts.Values.Where(a => filter.Role is null || a.Roles.Contains(filter.Role)).OrderBy(a => a.FullName).ToList();
        return Task.FromResult(Result.Success<IReadOnlyList<StaffAccount>>(rows));
    }

    public Task<Result<StaffAccount>> GetAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(_accounts.TryGetValue(id, out var account) ? Result.Success(account) : Result.Failure<StaffAccount>(Error.NotFound("Identity.UserNotFound", "No such user.")));

    public Task<Result<StaffInvite>> InviteAsync(InviteStaff request, CancellationToken cancellationToken)
    {
        var id = "staff-" + Guid.NewGuid().ToString("N")[..6];
        Add(id, request.FullName, request.Role, request.Email);
        _accounts[id] = _accounts[id] with { StationCodes = request.StationCodes };
        return Task.FromResult(Result.Success(new StaffInvite(_accounts[id], false, null)));
    }

    public Task<Result<StaffAccount>> UpdateAsync(string id, UpdateStaff request, CancellationToken cancellationToken)
    {
        var current = _accounts[id];
        _accounts[id] = current with { Roles = request.Role is null ? current.Roles : [request.Role] };
        return Task.FromResult(Result.Success(_accounts[id]));
    }

    public Task<Result> SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken)
    {
        _accounts[id] = _accounts[id] with { Status = enabled ? "Active" : "Disabled" };
        return Task.FromResult(Result.Success());
    }

    public Task<Result> UnlockAsync(string id, CancellationToken cancellationToken) => Task.FromResult(Result.Success());

    public Task<Result<StaffStats>> GetStatsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new StaffStats(_accounts.Count, new Dictionary<string, int>(), 0, 0, 0, 0, _accounts.Values.Count(a => a.Status == "Disabled"))));

    public Task<IReadOnlyDictionary<string, string>> GetNamesAsync(IEnumerable<string> subjects, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<string, string>>(subjects.Distinct().ToDictionary(s => s, s => _accounts.TryGetValue(s, out var a) ? a.FullName : $"Person {s}"));

    public Task<Result> ApplyLockoutAsync(int failures, int lockMinutes, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}

public sealed class AdminApiFactory : NetworkApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            var directory = new InMemoryStaffDirectory();
            services.RemoveAll<IStaffDirectory>();
            services.RemoveAll<IStaffNames>();
            services.AddSingleton<IStaffDirectory>(directory);
            services.AddSingleton<IStaffNames>(directory);
        });
    }

    internal HttpClient As(string role, string subject) =>
        CreateAuthenticatedClient(Tokens.Issue(subject: subject, userName: subject, roles: [role]));
}

public sealed class AdminApiTests(AdminApiFactory factory) : IClassFixture<AdminApiFactory>
{
    private HttpClient Admin => factory.As("admin", "admin-a");

    private static StringContent Body(object value) => new(JsonSerializer.Serialize(value), System.Text.Encoding.UTF8, "application/json");

    private static async Task<JsonElement> Ok(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.AbsolutePath} gave {(int)response.StatusCode}: {text}");
        return string.IsNullOrWhiteSpace(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
    }

    [Fact]
    public async Task The_administrator_invites_staff_changes_a_role_and_disables_access_and_each_change_is_audited()
    {
        var invited = await Ok(await Admin.PostAsync("/api/admin/users", Body(new { fullName = "Amaya Fonseka", email = "amaya@example.test", role = "scheduler" })));
        var id = invited.GetProperty("account").GetProperty("id").GetString();

        // a station master must have a station; an unknown role is refused
        Assert.Equal(HttpStatusCode.BadRequest, (await Admin.PostAsync("/api/admin/users", Body(new { fullName = "Bad Role", email = "bad@example.test", role = "station_master" }))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Admin.PostAsync("/api/admin/users", Body(new { fullName = "Bad Role", email = "bad2@example.test", role = "emperor" }))).StatusCode);

        await Ok(await Admin.PatchAsync($"/api/admin/users/{id}", Body(new { role = "controller", reason = "Moved to the control room" })));
        Assert.Equal(HttpStatusCode.NoContent, (await Admin.PostAsync($"/api/admin/users/{id}/disable", Body(new { reason = "Left the company" }))).StatusCode);

        var audit = await Ok(await Admin.GetAsync("/api/audit?kind=access"));
        var actions = audit.EnumerateArray().Select(e => e.GetProperty("action").GetString()).ToList();
        Assert.Contains("user.invite", actions);
        Assert.Contains("user.role.change", actions);
        Assert.Contains("user.disable", actions);
    }

    [Fact]
    public async Task The_controller_and_the_scheduler_cannot_manage_users()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.As("controller", "ctrl-a").GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.As("scheduler", "sched-a").PostAsync("/api/admin/users", Body(new { fullName = "x", email = "x@example.test", role = "driver" }))).StatusCode);
    }

    [Fact]
    public async Task A_device_is_registered_listed_and_revoked_with_a_reason()
    {
        var created = await Ok(await Admin.PostAsync("/api/admin/devices", Body(new { name = "Platform 1 display", type = "PlatformDisplay", stationCode = "MIL", location = "Platform 1" })));
        var id = created.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.BadRequest, (await Admin.PostAsync("/api/admin/devices", Body(new { name = "Nowhere", type = "PlatformDisplay", stationCode = "ZZZ" }))).StatusCode);

        var list = await Ok(await Admin.GetAsync("/api/admin/devices"));
        Assert.Contains(list.GetProperty("items").EnumerateArray(), d => d.GetProperty("id").GetGuid() == id);

        Assert.Equal(HttpStatusCode.BadRequest, (await Admin.PostAsync($"/api/admin/devices/{id}/revoke", Body(new { reason = " " }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Admin.PostAsync($"/api/admin/devices/{id}/revoke", Body(new { reason = "Stolen" }))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Admin.PostAsync($"/api/admin/devices/{id}/revoke", Body(new { reason = "Again" }))).StatusCode);
    }

    [Fact]
    public async Task Only_the_administrator_registers_devices_but_a_controller_can_read_their_health()
    {
        var controller = factory.As("controller", "ctrl-a");
        Assert.Equal(HttpStatusCode.OK, (await controller.GetAsync("/api/admin/devices")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await controller.PostAsync("/api/admin/devices", Body(new { name = "x", type = "PlatformDisplay", stationCode = "MIL" }))).StatusCode);
    }

    [Fact]
    public async Task Security_settings_are_validated_saved_and_audited()
    {
        var current = await Ok(await Admin.GetAsync("/api/admin/security"));
        var settings = current.GetProperty("settings");
        Assert.True(settings.GetProperty("lockAfterFailures").GetInt32() >= 3);

        Assert.Equal(HttpStatusCode.BadRequest, (await Admin.PutAsync("/api/admin/security", Body(new { lockAfterFailures = 1 }))).StatusCode);

        await Ok(await Admin.PutAsync("/api/admin/security", Body(new { lockAfterFailures = 6, lockMinutes = 20, twoStepRequired = true, consoleIdleMinutes = 15, driverAppLockMinutes = 5, accessReviewDays = 90, flagLeavers = true, deviceLateAfterSeconds = 60, deviceOfflineAfterSeconds = 90 })));
        var after = await Ok(await Admin.GetAsync("/api/admin/security"));
        Assert.Equal(6, after.GetProperty("settings").GetProperty("lockAfterFailures").GetInt32());

        Assert.Equal(HttpStatusCode.Forbidden, (await factory.As("controller", "ctrl-a").GetAsync("/api/admin/security")).StatusCode);

        var audit = await Ok(await Admin.GetAsync("/api/audit?kind=access&action=security.settings.update"));
        Assert.NotEmpty(audit.EnumerateArray());
    }

    [Fact]
    public async Task The_dashboard_and_the_role_matrix_are_for_the_administrator()
    {
        var dashboard = await Ok(await Admin.GetAsync("/api/admin/dashboard"));
        Assert.True(dashboard.TryGetProperty("needsAttention", out _));

        var roles = await Ok(await Admin.GetAsync("/api/admin/roles"));
        Assert.True(roles.GetRawText().Contains("scheduler", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.As("scheduler", "sched-a").GetAsync("/api/admin/roles")).StatusCode);
    }
}
