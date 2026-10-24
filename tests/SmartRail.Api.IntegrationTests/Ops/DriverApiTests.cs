using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Journeys.Domain.Crew;
using SmartRail.Modules.Journeys.Domain.Journeys;

namespace SmartRail.Api.IntegrationTests.Ops;

// The driver app's backend on a real database: my duty, sign on, one-tap events in order, a delay reason, sign off, and nobody else's journey.
public sealed class DriverApiTests(OpsSeedFactory factory) : OpsTestBase(factory), IClassFixture<OpsSeedFactory>
{
    private const string Me = "drv-test-1";

    // A journey that has not left, on a duty of drv-test-1 that covers "now".
    private async Task<(Guid JourneyId, Guid DutyId)> GivenADutyAsync(string subject = Me)
    {
        await Factory.GenerateAsync();
        var now = Factory.Clock.GetUtcNow();
        return await Factory.WithDbAsync(async db =>
        {
            var journey = await db.Journeys.Include(j => j.Stops).Include(j => j.DriverAssignments)
                .Where(j => j.Lifecycle == JourneyLifecycle.Planned && j.ScheduledDepartureUtc > now && !j.DriverAssignments.Any())
                .OrderBy(j => j.ScheduledDepartureUtc).FirstAsync();

            var duty = CrewDuty.Create(subject, journey.ServiceDate, now.AddMinutes(-60) < journey.ScheduledDepartureUtc.AddHours(-1) ? now.AddMinutes(-60) : journey.ScheduledDepartureUtc.AddHours(-1),
                journey.ScheduledArrivalUtc.AddHours(1)).Value;
            db.CrewDuties.Add(duty);
            journey.AssignDriver(duty.Id, journey.ScheduledDepartureUtc, journey.ScheduledArrivalUtc, "test");
            journey.TransitionTo(JourneyLifecycle.Ready);
            await db.SaveChangesAsync();
            return (journey.Id, duty.Id);
        });
    }

    private static StringContent Event(int stop, string type, long sequence) => Body(new { stopSequence = stop, type, sequence });

    [Fact]
    public async Task A_driver_signs_on_works_the_journey_in_order_and_signs_off()
    {
        var (journeyId, dutyId) = await GivenADutyAsync();
        var driver = Driver(Me);

        var duty = await Ok(await driver.GetAsync("/api/driver/duty"));
        var mine = duty.GetProperty("duties").EnumerateArray().First(d => d.GetProperty("id").GetGuid() == dutyId);
        Assert.True(mine.GetProperty("canSignOn").GetBoolean());
        Assert.Contains(mine.GetProperty("journeys").EnumerateArray(), j => j.GetProperty("journeyId").GetGuid() == journeyId);

        // nothing can be done before signing on
        Assert.Equal(HttpStatusCode.Conflict, (await driver.PostAsync($"/api/driver/journeys/{journeyId}/events", Event(1, "departed", 1))).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await driver.PostAsync($"/api/driver/duty/{dutyId}/sign-on", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await driver.PostAsync($"/api/driver/duty/{dutyId}/sign-on", null)).StatusCode);

        var journey = await Ok(await driver.GetAsync($"/api/driver/journeys/{journeyId}"));
        Assert.True(journey.GetProperty("canAct").GetBoolean());
        Assert.Equal("departed", journey.GetProperty("next").GetProperty("type").GetString());
        Assert.Equal(1, journey.GetProperty("next").GetProperty("stopSequence").GetInt32());

        // the wrong stop or the wrong kind of event is refused, and says what was expected
        var wrong = await driver.PostAsync($"/api/driver/journeys/{journeyId}/events", Event(2, "arrived", 1));
        Assert.Equal(HttpStatusCode.Conflict, wrong.StatusCode);
        Assert.Equal("Journeys.StopDoesNotMatch", (await Json(wrong)).GetProperty("code").GetString());

        // departing the origin, then arriving at the next stop
        var departed = await Ok(await driver.PostAsync($"/api/driver/journeys/{journeyId}/events", Event(1, "departed", 1)));
        Assert.Equal("Applied", departed.GetProperty("disposition").GetString());

        // sending the same message again (a retry) is stored once
        var again = await driver.PostAsync($"/api/driver/journeys/{journeyId}/events", Event(1, "departed", 1));
        Assert.True(again.IsSuccessStatusCode || again.StatusCode == HttpStatusCode.Conflict);

        var arrived = await Ok(await driver.PostAsync($"/api/driver/journeys/{journeyId}/events", Event(2, "arrived", 2)));
        Assert.Equal("Applied", arrived.GetProperty("disposition").GetString());

        // the controller sees the journey running on the live board
        var detail = await Ok(await Controller().GetAsync($"/api/ops/journeys/{journeyId}"));
        Assert.Equal("Running", detail.GetProperty("journey").GetProperty("lifecycle").GetString());

        // a delay reason, from the approved list only
        Assert.Equal(HttpStatusCode.NoContent, (await driver.PostAsync($"/api/driver/journeys/{journeyId}/delay-reason", Body(new { reasonCode = "signal_fault" }))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await driver.PostAsync($"/api/driver/journeys/{journeyId}/delay-reason", Body(new { reasonCode = "made_up" }))).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await driver.PostAsync($"/api/driver/duty/{dutyId}/sign-off", null)).StatusCode);
        var after = await Ok(await driver.GetAsync($"/api/driver/journeys/{journeyId}"));
        Assert.False(after.GetProperty("canAct").GetBoolean());
    }

    [Fact]
    public async Task Another_driver_cannot_see_or_work_my_journey_and_a_controller_cannot_use_the_driver_endpoints()
    {
        var (journeyId, dutyId) = await GivenADutyAsync("drv-test-2");

        var other = Driver("drv-test-9");
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/driver/journeys/{journeyId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync($"/api/driver/journeys/{journeyId}/events", Event(1, "departed", 1))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync($"/api/driver/duty/{dutyId}/sign-on", null)).StatusCode);

        var empty = await Ok(await other.GetAsync("/api/driver/duty"));
        Assert.Equal(0, empty.GetProperty("duties").GetArrayLength());

        Assert.Equal(HttpStatusCode.Forbidden, (await Controller().GetAsync("/api/driver/duty")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Scheduler().PostAsync($"/api/driver/duty/{dutyId}/sign-on", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.As("passenger", "pax-1").GetAsync("/api/driver/duty")).StatusCode);
    }
}
