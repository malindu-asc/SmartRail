using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Modules.Network.Domain.Routes;
using SmartRail.Modules.Network.Domain.Stations;
using SmartRail.Modules.Network.Infrastructure.Extensions;
using SmartRail.Modules.Network.Infrastructure.Persistence;

namespace SmartRail.Api.IntegrationTests.Network;

public sealed class NetworkSeedTests(NetworkApiFactory factory) : IClassFixture<NetworkApiFactory>
{
    // AC-7
    [Fact]
    public async Task Seeding_again_adds_nothing_and_never_overwrites_edits()
    {
        using var client = factory.CreateClient(); // starts the host: migrations and first seed run here

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NetworkDbContext>();
            await db.Database.ExecuteSqlRawAsync("UPDATE network.stations SET name = 'Central Station' WHERE code = 'CEN'");
        }

        await factory.Services.SeedNetworkReferenceDataAsync();

        await using var check = factory.Services.CreateAsyncScope();
        var context = check.ServiceProvider.GetRequiredService<NetworkDbContext>();
        Assert.Equal(12, await context.Stations.CountAsync());
        Assert.Equal(4, await context.Routes.CountAsync());
        Assert.Equal(2, await context.Languages.CountAsync());
        Assert.Equal("Central Station", await context.Stations.Where(s => s.Code == "CEN").Select(s => s.Name).SingleAsync());
    }
}

public sealed class NetworkConstraintTests(NetworkApiFactory factory) : IClassFixture<NetworkApiFactory>
{
    // AC-11
    [Fact]
    public async Task Database_refuses_a_default_platform_from_another_station()
    {
        using var client = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NetworkDbContext>();

        var stationA = Station.Create("TSA", "Test A", []).Value;
        var platformA = stationA.AddPlatform("1").Value;
        var stationB = Station.Create("TSB", "Test B", []).Value;
        stationB.AddPlatform("1");
        db.Stations.AddRange(stationA, stationB);
        await db.SaveChangesAsync();

        var route = Route.Create("BADRT", "Bad route",
        [
            new RouteStopDraft(1, stationA.Id, platformA.Id, 0, 1, StopKind.Stop),
            // A platform that belongs to station A, used at station B.
            new RouteStopDraft(2, stationB.Id, platformA.Id, 5, 1, StopKind.Stop)
        ]).Value;
        db.Routes.Add(route);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}

public sealed class NetworkSeedOffTests(UnseededNetworkApiFactory factory) : IClassFixture<UnseededNetworkApiFactory>
{
    // AC-8
    [Fact]
    public async Task Nothing_is_seeded_unless_the_flag_is_on()
    {
        using var client = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NetworkDbContext>();
        Assert.Equal(0, await db.Stations.CountAsync());
        Assert.Equal(0, await db.Languages.CountAsync());
    }
}
