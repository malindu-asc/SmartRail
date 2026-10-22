using System.Net.Http.Json;
using System.Text.Json;
using SmartRail.Api.IntegrationTests.Ops;

namespace SmartRail.Api.IntegrationTests.Journeys;

// The real API on a real Postgres with the demo network and timetable, and no pre-assigned drivers: the scheduler plans the week.
public sealed class WeekPlanApiFactory : OpsApiFactory
{
    protected override bool DemoFleet => false;

    protected override bool ApprovalRequired => true;
}

public sealed class WeekPlanTests(WeekPlanApiFactory factory) : IClassFixture<WeekPlanApiFactory>
{
    // The first demo driver of the Keycloak realm; the plan gives them the MAIN-F trainset.
    private const string DemoDriver = "a1000000-0000-4000-8000-000000000002";
    private const string Day = "2026-10-14";

    private static async Task<JsonElement> Ok(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage?.RequestUri?.AbsolutePath} gave {(int)response.StatusCode}: {text}");
        return string.IsNullOrWhiteSpace(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
    }

    [Fact]
    public async Task The_scheduler_generates_the_week_and_the_driver_sees_it_only_after_it_is_approved()
    {
        await factory.GenerateAsync();
        var scheduler = factory.As("scheduler", "sched-a");
        var driver = factory.As("driver", DemoDriver);

        // Nothing is assigned before the plan is generated.
        var before = await Ok(await scheduler.GetAsync($"/api/ops/planning/week?from={Day}"));
        var firstDay = before.GetProperty("days")[0];
        Assert.True(firstDay.GetProperty("journeys").GetInt32() > 0);
        Assert.Equal(firstDay.GetProperty("journeys").GetInt32(), firstDay.GetProperty("unassigned").GetInt32());
        Assert.False(firstDay.GetProperty("approved").GetBoolean());

        // Generate: the demo drivers get their trainsets, and the rest is left for the scheduler.
        var generated = await Ok(await scheduler.PostAsync($"/api/ops/planning/generate?from={Day}", null));
        Assert.True(generated.GetProperty("assigned").GetInt32() > 0);

        var after = await Ok(await scheduler.GetAsync($"/api/ops/planning/week?from={Day}"));
        Assert.True(after.GetProperty("days")[0].GetProperty("unassigned").GetInt32() < firstDay.GetProperty("unassigned").GetInt32());

        // Generating again changes nothing.
        var again = await Ok(await scheduler.PostAsync($"/api/ops/planning/generate?from={Day}", null));
        Assert.Equal(0, again.GetProperty("assigned").GetInt32());

        // Drafted but not approved: the driver has no duty yet.
        var hidden = await Ok(await driver.GetAsync("/api/driver/duty"));
        Assert.Equal(0, hidden.GetProperty("duties").GetArrayLength());

        // Approved: the duties appear.
        var approved = await Ok(await scheduler.PostAsync($"/api/ops/planning/approve?from={Day}", null));
        Assert.Equal(7, approved.GetProperty("days").GetInt32());

        var visible = await Ok(await driver.GetAsync("/api/driver/duty"));
        Assert.True(visible.GetProperty("duties").GetArrayLength() > 0);

        var week = await Ok(await scheduler.GetAsync($"/api/ops/planning/week?from={Day}"));
        Assert.True(week.GetProperty("days")[0].GetProperty("approved").GetBoolean());
    }

    [Fact]
    public async Task A_controller_cannot_generate_or_approve_the_plan()
    {
        await factory.GenerateAsync();
        var controller = factory.As("controller", "ctrl-a");

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, (await controller.PostAsync($"/api/ops/planning/generate?from={Day}", null)).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, (await controller.PostAsync($"/api/ops/planning/approve?from={Day}", null)).StatusCode);
    }
}
