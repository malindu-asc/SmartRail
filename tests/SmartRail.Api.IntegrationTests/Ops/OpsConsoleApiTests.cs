using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Api.IntegrationTests.Journeys;
using SmartRail.Modules.Identity.Contracts;

namespace SmartRail.Api.IntegrationTests.Ops;

// People known to the tests, so no identity provider is needed.
internal sealed class FakeStaffNames : IStaffNames
{
    public Task<IReadOnlyDictionary<string, string>> GetNamesAsync(IEnumerable<string> subjects, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<string, string>>(subjects.Distinct().ToDictionary(s => s, s => $"Person {s}"));
}

// The real API on a real Postgres with the demo network, timetable and fleet: the controller, scheduler and driver consoles end to end.
public sealed class OpsApiFactory : JourneysApiFactory
{
    protected override bool DemoFleet => true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IStaffNames>();
            services.AddScoped<IStaffNames, FakeStaffNames>();
        });
    }

    internal HttpClient As(string role, string subject) =>
        CreateAuthenticatedClient(Tokens.Issue(subject: subject, userName: subject, roles: [role]));
}

public abstract class OpsTestBase(OpsApiFactory factory)
{
    protected OpsApiFactory Factory { get; } = factory;

    protected HttpClient Controller(string who = "ctrl-a") => Factory.As("controller", who);

    protected HttpClient Scheduler(string who = "sched-a") => Factory.As("scheduler", who);

    protected HttpClient Driver(string who = "drv-1") => Factory.As("driver", who);

    protected HttpClient Admin(string who = "admin-a") => Factory.As("admin", who);

    protected static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
    }

    protected static async Task<JsonElement> Ok(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.AbsolutePath} gave {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return await Json(response);
    }

    protected async Task<JsonElement> Board(HttpClient client)
    {
        await Factory.GenerateAsync();
        return await Ok(await client.GetAsync("/api/ops/journeys?date=2026-10-14"));
    }

    // A journey that has not left yet, so it can be cancelled.
    protected async Task<(Guid Id, int Version)> ScheduledJourney(int skip = 0)
    {
        var board = await Board(Controller());
        var item = board.GetProperty("items").EnumerateArray()
            .Where(i => i.GetProperty("lifecycle").GetString() is "Planned" or "Ready")
            .Skip(skip).First();
        return (item.GetProperty("journeyId").GetGuid(), item.GetProperty("version").GetInt32());
    }

    protected static StringContent Body(object value) => new(JsonSerializer.Serialize(value), System.Text.Encoding.UTF8, "application/json");
}

public sealed class ControllerJourneyTests(OpsApiFactory factory) : OpsTestBase(factory), IClassFixture<OpsApiFactory>
{
    [Fact]
    public async Task The_board_lists_todays_journeys_with_state_and_what_needs_attention()
    {
        var board = await Board(Controller());

        Assert.True(board.GetProperty("total").GetInt32() > 0);
        var first = board.GetProperty("items")[0];
        Assert.False(string.IsNullOrEmpty(first.GetProperty("trainNumber").GetString()));
        Assert.True(first.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task A_cancellation_needs_a_reason_and_the_current_version_and_is_audited()
    {
        var (id, version) = await ScheduledJourney();
        var client = Controller();

        var noReason = await client.PostAsync($"/api/ops/journeys/{id}/cancel", Body(new { expectedVersion = version }));
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);

        var stale = await client.PostAsync($"/api/ops/journeys/{id}/cancel", Body(new { reasonCode = "signal_fault", expectedVersion = version + 50 }));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        await Ok(await client.PostAsync($"/api/ops/journeys/{id}/cancel", Body(new { reasonCode = "signal_fault", note = "Signal failure at the junction", expectedVersion = version })));

        var detail = await Ok(await client.GetAsync($"/api/ops/journeys/{id}"));
        Assert.Equal("Cancelled", detail.GetProperty("journey").GetProperty("lifecycle").GetString());

        // the controller reads the operational audit log; it shows who cancelled and why
        var audit = await Ok(await client.GetAsync("/api/audit?kind=operational&action=journey.cancel"));
        Assert.Contains(audit.EnumerateArray(), e => e.GetProperty("targetId").GetString() == id.ToString() && e.GetProperty("actorSubject").GetString() == "ctrl-a");
    }

    [Fact]
    public async Task A_platform_change_is_checked_and_recorded()
    {
        var (id, version) = await ScheduledJourney(1);
        var client = Controller();

        var detail = await Ok(await client.GetAsync($"/api/ops/journeys/{id}"));
        var stop = detail.GetProperty("stops")[0];
        var sequence = stop.GetProperty("sequence").GetInt32();
        var current = stop.GetProperty("platform").GetString();

        var check = await client.GetAsync($"/api/ops/journeys/{id}/platform-check?stopSequence={sequence}&platform={current}");
        Assert.True(check.IsSuccessStatusCode);

        var impact = await Ok(await client.GetAsync($"/api/ops/journeys/{id}/impact?action=platform&stopSequence={sequence}"));
        Assert.Equal("platform", impact.GetProperty("action").GetString());
    }

    [Fact]
    public async Task The_scheduler_cannot_cancel_and_a_passenger_cannot_open_the_console()
    {
        var (id, version) = await ScheduledJourney(2);
        var cancel = await Scheduler().PostAsync($"/api/ops/journeys/{id}/cancel", Body(new { reasonCode = "weather", expectedVersion = version }));
        Assert.Equal(HttpStatusCode.Forbidden, cancel.StatusCode);

        var passenger = Factory.As("passenger", "pax-1");
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.GetAsync("/api/ops/dashboard")).StatusCode);
    }

    [Fact]
    public async Task The_dashboard_counts_the_network_and_lists_what_needs_attention()
    {
        await Factory.GenerateAsync();
        var dashboard = await Ok(await Controller().GetAsync("/api/ops/dashboard"));

        Assert.True(dashboard.GetProperty("kpis").TryGetProperty("running", out _));
        Assert.True(dashboard.TryGetProperty("needsAttention", out _));
        Assert.True(dashboard.GetProperty("devices").TryGetProperty("total", out _));
    }
}

public sealed class DisruptionWorkflowTests(OpsApiFactory factory) : OpsTestBase(factory), IClassFixture<OpsApiFactory>
{
    private static object Notice(bool networkWide, bool submit = true) => new
    {
        title = "Signal fault", message = "Delays of up to 20 minutes", severity = "Major", networkWide,
        stationCodes = networkWide ? Array.Empty<string>() : new[] { "MIL" }, type = "Infrastructure fault",
        reviewByUtc = DateTimeOffset.Parse("2026-10-14T08:00:00Z"), submit
    };

    [Fact]
    public async Task A_network_wide_notice_waits_for_a_second_person_and_then_reaches_passengers()
    {
        var author = Controller("ctrl-a");
        var created = await Ok(await author.PostAsync("/api/ops/disruptions", Body(Notice(true))));
        var id = created.GetProperty("id").GetGuid();
        Assert.Equal("AwaitingApproval", created.GetProperty("status").GetString());

        // passengers do not see it yet
        var before = await Ok(await Factory.CreateClient().GetAsync("/api/operations/disruptions"));
        Assert.DoesNotContain(before.EnumerateArray(), d => d.GetProperty("id").GetGuid() == id);

        // the author cannot approve their own notice
        var self = await author.PostAsync($"/api/ops/disruptions/{id}/approve", null);
        Assert.False(self.IsSuccessStatusCode);

        // a second controller approves
        Assert.Equal(HttpStatusCode.NoContent, (await Controller("ctrl-b").PostAsync($"/api/ops/disruptions/{id}/approve", null)).StatusCode);

        var after = await Ok(await Factory.CreateClient().GetAsync("/api/operations/disruptions"));
        Assert.Contains(after.EnumerateArray(), d => d.GetProperty("id").GetGuid() == id);

        // a wrong notice is retracted and disappears
        Assert.Equal(HttpStatusCode.NoContent, (await author.PostAsync($"/api/ops/disruptions/{id}/retract", Body(new { reason = "Wrong line" }))).StatusCode);
        var gone = await Ok(await Factory.CreateClient().GetAsync("/api/operations/disruptions"));
        Assert.DoesNotContain(gone.EnumerateArray(), d => d.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task Rejecting_needs_a_comment_and_sends_the_notice_back()
    {
        var created = await Ok(await Controller("ctrl-a").PostAsync("/api/ops/disruptions", Body(Notice(true))));
        var id = created.GetProperty("id").GetGuid();
        var other = Controller("ctrl-b");

        Assert.Equal(HttpStatusCode.BadRequest, (await other.PostAsync($"/api/ops/disruptions/{id}/reject", Body(new { comment = " " }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await other.PostAsync($"/api/ops/disruptions/{id}/reject", Body(new { comment = "Say which line" }))).StatusCode);

        var detail = await Ok(await other.GetAsync($"/api/ops/disruptions/{id}"));
        Assert.Equal("Rejected", detail.GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_local_notice_publishes_at_once_and_a_draft_stays_private()
    {
        var author = Controller();
        var local = await Ok(await author.PostAsync("/api/ops/disruptions", Body(Notice(false))));
        Assert.Equal("Published", local.GetProperty("status").GetString());

        var draft = await Ok(await author.PostAsync("/api/ops/disruptions", Body(Notice(false, submit: false))));
        var draftId = draft.GetProperty("id").GetGuid();
        Assert.Equal("Draft", draft.GetProperty("status").GetString());

        var list = await Ok(await Factory.CreateClient().GetAsync("/api/operations/disruptions"));
        Assert.DoesNotContain(list.EnumerateArray(), d => d.GetProperty("id").GetGuid() == draftId);
    }

    [Fact]
    public async Task The_scheduler_announces_planned_works_but_cannot_publish_a_live_notice()
    {
        var scheduler = Scheduler();
        var start = DateTimeOffset.Parse("2026-10-20T00:00:00Z");

        var live = await scheduler.PostAsync("/api/ops/disruptions", Body(Notice(false)));
        Assert.Equal(HttpStatusCode.Forbidden, live.StatusCode);

        var noStart = await scheduler.PostAsync("/api/ops/planned-works", Body(Notice(false)));
        Assert.Equal(HttpStatusCode.BadRequest, noStart.StatusCode);

        var planned = await Ok(await scheduler.PostAsync("/api/ops/planned-works", Body(new
        {
            title = "Track works", message = "No trains after 22:00", severity = "Minor", networkWide = false, stationCodes = new[] { "MIL" },
            startsAtUtc = start, expectedEndUtc = start.AddHours(8), reviewByUtc = start.AddHours(9), submit = true
        })));
        Assert.Equal("Published", planned.GetProperty("status").GetString());
    }
}

public sealed class IncidentAndMessageTests(OpsApiFactory factory) : OpsTestBase(factory), IClassFixture<OpsApiFactory>
{
    [Fact]
    public async Task A_critical_incident_needs_a_confirmation_and_a_reason_then_can_be_handed_over_and_resolved()
    {
        var ctrl = Controller("ctrl-a");

        var unconfirmed = await ctrl.PostAsync("/api/ops/incidents", Body(new { title = "Person on track", severity = "Critical", stationCodes = new[] { "MIL" } }));
        Assert.Equal(HttpStatusCode.BadRequest, unconfirmed.StatusCode);

        var incident = await Ok(await ctrl.PostAsync("/api/ops/incidents",
            Body(new { title = "Person on track", severity = "Critical", stationCodes = new[] { "MIL" }, confirm = true, reason = "Reported by the signaller" })));
        var id = incident.GetProperty("incident").GetProperty("id").GetGuid();

        // the sign-out check says this controller still owns it
        var check = await Ok(await ctrl.GetAsync("/api/ops/incidents/sign-out-check"));
        Assert.True(check.GetProperty("mustHandOver").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await ctrl.PostAsync($"/api/ops/incidents/{id}/handover", Body(new { newOwnerSubject = "ctrl-b", note = "Shift change" }))).StatusCode);
        Assert.False((await Ok(await ctrl.GetAsync("/api/ops/incidents/sign-out-check"))).GetProperty("mustHandOver").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await Controller("ctrl-b").PostAsync($"/api/ops/incidents/{id}/resolve", Body(new { note = "Track clear" }))).StatusCode);
        var detail = await Ok(await ctrl.GetAsync($"/api/ops/incidents/{id}"));
        Assert.Equal("Resolved", detail.GetProperty("incident").GetProperty("status").GetString());
    }

    [Fact]
    public async Task The_scheduler_and_the_driver_cannot_declare_incidents()
    {
        var body = Body(new { title = "x", severity = "Minor" });
        Assert.Equal(HttpStatusCode.Forbidden, (await Scheduler().PostAsync("/api/ops/incidents", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Driver().PostAsync("/api/ops/incidents", Body(new { title = "x", severity = "Minor" }))).StatusCode);
    }

    [Fact]
    public async Task A_driver_gets_only_their_own_messages_and_acknowledges_them()
    {
        var ctrl = Controller();
        var sent = await Ok(await ctrl.PostAsync("/api/ops/messages", Body(new
        {
            driverSubject = "drv-1", body = "Hold at Mill Lane until cleared", requiresAck = true, validUntilUtc = DateTimeOffset.UtcNow.AddHours(4)
        })));
        var id = sent.GetProperty("id").GetGuid();

        var mine = await Ok(await Driver("drv-1").GetAsync("/api/driver/messages"));
        Assert.Contains(mine.EnumerateArray(), m => m.GetProperty("id").GetGuid() == id);

        var theirs = await Ok(await Driver("drv-2").GetAsync("/api/driver/messages"));
        Assert.DoesNotContain(theirs.EnumerateArray(), m => m.GetProperty("id").GetGuid() == id);

        // another driver cannot acknowledge it: it looks like it does not exist
        Assert.Equal(HttpStatusCode.NotFound, (await Driver("drv-2").PostAsync($"/api/driver/messages/{id}/acknowledge", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Driver("drv-1").PostAsync($"/api/driver/messages/{id}/acknowledge", null)).StatusCode);

        var one = await Ok(await ctrl.GetAsync($"/api/ops/messages/{id}"));
        Assert.Equal("Acknowledged", one.GetProperty("state").GetString());
    }

    [Fact]
    public async Task An_assistance_request_reaches_the_dashboard_until_a_controller_handles_it()
    {
        var driver = Driver("drv-3");
        var request = await Ok(await driver.PostAsync("/api/driver/assistance", Body(new { stationCode = "MIL", note = "Unwell passenger" })));
        var id = request.GetProperty("id").GetGuid();

        var ctrl = Controller();
        var before = await Ok(await ctrl.GetAsync("/api/ops/dashboard"));
        Assert.Contains(before.GetProperty("needsAttention").EnumerateArray(), a => a.GetProperty("kind").GetString() == "AssistanceRequested" && a.GetProperty("targetId").GetString() == id.ToString());

        Assert.Equal(HttpStatusCode.NoContent, (await ctrl.PostAsync($"/api/ops/assistance/{id}/handle", Body(new { outcome = "Ambulance called" }))).StatusCode);
        var after = await Ok(await ctrl.GetAsync("/api/ops/dashboard"));
        Assert.DoesNotContain(after.GetProperty("needsAttention").EnumerateArray(), a => a.GetProperty("targetId").GetString() == id.ToString());
    }
}

public sealed class AuditAccessTests(OpsApiFactory factory) : OpsTestBase(factory), IClassFixture<OpsApiFactory>
{
    [Fact]
    public async Task The_scheduler_cannot_read_the_audit_log_and_the_administrator_reads_only_access_changes()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Scheduler().GetAsync("/api/audit")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Admin().GetAsync("/api/audit?kind=operational")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Admin().GetAsync("/api/audit")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Controller().GetAsync("/api/audit")).StatusCode);
    }
}

public sealed class TimetableWorkflowTests(OpsApiFactory factory) : OpsTestBase(factory), IClassFixture<OpsApiFactory>
{
    [Fact]
    public async Task A_draft_is_edited_reviewed_by_another_scheduler_and_replaces_the_version_in_force()
    {
        var author = Scheduler("sched-a");
        var versions = await Ok(await author.GetAsync("/api/ops/timetable/versions"));
        var inForce = versions.GetProperty("items").EnumerateArray().First(v => v.GetProperty("inForce").GetBoolean());
        var oldId = inForce.GetProperty("id").GetGuid();

        var draft = await Ok(await author.PostAsync("/api/ops/timetable/versions", Body(new { label = "Winter 2026" })));
        var draftId = draft.GetProperty("version").GetProperty("id").GetGuid();
        Assert.Equal("Draft", draft.GetProperty("version").GetProperty("status").GetString());
        Assert.Equal(inForce.GetProperty("services").GetInt32(), draft.GetProperty("services").GetArrayLength());

        // only one open draft at a time
        Assert.Equal(HttpStatusCode.Conflict, (await author.PostAsync("/api/ops/timetable/versions", Body(new { label = "Another" }))).StatusCode);

        // move the first service five minutes later
        var service = draft.GetProperty("services")[0];
        var stops = service.GetProperty("stops").EnumerateArray().Select(s => new
        {
            stationCode = s.GetProperty("stationCode").GetString(),
            arrival = Shift(s, "arrivalMinutes"),
            departure = Shift(s, "departureMinutes")
        }).ToList();
        Assert.Equal(HttpStatusCode.NoContent, (await author.PutAsync($"/api/ops/timetable/versions/{draftId}/services/{service.GetProperty("id").GetGuid()}/times", Body(new { stops }))).StatusCode);

        var comparison = await Ok(await author.GetAsync($"/api/ops/timetable/versions/{draftId}/compare"));
        Assert.Equal(1, comparison.GetProperty("changed").GetInt32());

        Assert.Equal(HttpStatusCode.NoContent, (await author.PostAsync($"/api/ops/timetable/versions/{draftId}/submit", null)).StatusCode);

        // the author cannot approve their own version; a past date is refused
        Assert.False((await author.PostAsync($"/api/ops/timetable/versions/{draftId}/approve", Body(new { effectiveFrom = "2027-01-04" }))).IsSuccessStatusCode);
        var other = Scheduler("sched-b");
        Assert.Equal(HttpStatusCode.BadRequest, (await other.PostAsync($"/api/ops/timetable/versions/{draftId}/approve", Body(new { effectiveFrom = "2020-01-01" }))).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await other.PostAsync($"/api/ops/timetable/versions/{draftId}/approve", Body(new { effectiveFrom = "2027-01-04" }))).StatusCode);

        var after = await Ok(await author.GetAsync("/api/ops/timetable/versions"));
        var items = after.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal("Published", items.First(v => v.GetProperty("id").GetGuid() == draftId).GetProperty("status").GetString());
        Assert.Equal("Superseded", items.First(v => v.GetProperty("id").GetGuid() == oldId).GetProperty("status").GetString());

        // a published version cannot be edited
        Assert.Equal(HttpStatusCode.Conflict, (await author.DeleteAsync($"/api/ops/timetable/versions/{draftId}/services/{service.GetProperty("id").GetGuid()}")).StatusCode);

        // the audit trail shows who published it
        var audit = await Ok(await Controller().GetAsync("/api/audit?kind=operational&action=timetable.publish"));
        Assert.Contains(audit.EnumerateArray(), e => e.GetProperty("actorSubject").GetString() == "sched-b");
    }

    private static string? Shift(JsonElement stop, string property)
    {
        var value = stop.GetProperty(property);
        if (value.ValueKind == JsonValueKind.Null)
            return null;

        var minutes = value.GetInt32() + 5;
        return $"{minutes / 60:00}:{minutes % 60:00}";
    }

    [Fact]
    public async Task The_controller_and_the_administrator_cannot_touch_the_timetable()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Controller().PostAsync("/api/ops/timetable/versions", Body(new { label = "x" }))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Admin().GetAsync("/api/ops/timetable/versions")).StatusCode);
    }
}
