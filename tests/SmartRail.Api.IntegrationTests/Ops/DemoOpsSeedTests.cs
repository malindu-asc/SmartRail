using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SmartRail.Api.Development;
using SmartRail.Modules.Identity.Infrastructure.Persistence;
using SmartRail.Modules.Journeys.Infrastructure.Persistence;
using SmartRail.Modules.Network.Contracts;
using SmartRail.Modules.Operations.Infrastructure.Persistence;
using SmartRail.Modules.Ticketing.Infrastructure.Persistence;

namespace SmartRail.Api.IntegrationTests.Ops;

// The scenario seeder lays out its own crews, so the fleet seeder stays off here.
public sealed class OpsSeedFactory : OpsApiFactory
{
    protected override bool DemoFleet => false;
}

// The demo database has to give every console something real to show, and running the seeder again must not duplicate anything.
public sealed class DemoOpsSeedTests(OpsSeedFactory factory) : OpsTestBase(factory), IClassFixture<OpsSeedFactory>
{
    private async Task SeedAsync()
    {
        await Factory.GenerateAsync();
        await using var scope = Factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var seeder = new DemoScenarioSeeder(
            sp.GetRequiredService<JourneysDbContext>(), sp.GetRequiredService<TicketingDbContext>(), sp.GetRequiredService<OperationsDbContext>(),
            sp.GetRequiredService<IdentityDbContext>(), sp.GetRequiredService<INetworkTimeZone>(), sp.GetRequiredService<TimeProvider>(),
            NullLogger<DemoScenarioSeeder>.Instance);
        await seeder.ApplyAsync();
    }

    [Fact]
    public async Task The_controller_console_has_work_on_a_fresh_demo_database_and_seeding_twice_adds_nothing()
    {
        await SeedAsync();
        var controller = Controller("a1000000-0000-4000-8000-000000000006");

        var disruptions = await Ok(await controller.GetAsync("/api/ops/disruptions"));
        Assert.True(disruptions.GetProperty("counts").GetProperty("active").GetInt32() >= 1);

        var incidents = await Ok(await controller.GetAsync("/api/ops/incidents?openOnly=true"));
        Assert.Contains(incidents.EnumerateArray(), i => i.GetProperty("title").GetString() == "Signal failure at Mill Lane");

        var dashboard = await Ok(await controller.GetAsync("/api/ops/dashboard"));
        var kinds = dashboard.GetProperty("needsAttention").EnumerateArray().Select(a => a.GetProperty("kind").GetString()).ToList();
        Assert.Contains("AssistanceRequested", kinds);
        Assert.Contains("MessageNotAcknowledged", kinds);

        var before = (await Ok(await controller.GetAsync("/api/ops/messages"))).GetProperty("items").GetArrayLength();
        await SeedAsync();
        var after = (await Ok(await controller.GetAsync("/api/ops/messages"))).GetProperty("items").GetArrayLength();
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task The_demo_driver_sees_the_message_waiting_for_an_acknowledgement()
    {
        await SeedAsync();
        var driver = Driver(DemoScenarioSeeder.DriverSubject);

        var messages = await Ok(await driver.GetAsync("/api/driver/messages"));
        Assert.Contains(messages.EnumerateArray(), m => m.GetProperty("requiresAck").GetBoolean() && m.GetProperty("state").GetString() != "Acknowledged");
    }

    [Fact]
    public async Task The_administrator_sees_a_device_register_with_online_late_and_offline_devices()
    {
        await SeedAsync();
        var devices = await Ok(await Admin().GetAsync("/api/admin/devices"));
        var health = devices.GetProperty("items").EnumerateArray().Select(d => d.GetProperty("health").GetString()).ToHashSet();

        Assert.Contains("Online", health);
        Assert.Contains("Late", health);
        Assert.Contains("Offline", health);
    }
}
