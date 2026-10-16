using SmartRail.Modules.Network.Domain.Routes;

namespace SmartRail.Modules.Network.Tests;

public sealed class RouteTests
{
    private static RouteStopDraft Stop(int sequence, Guid station, int minutes = 5, int dwell = 1, StopKind kind = StopKind.Stop) =>
        new(sequence, station, null, minutes, dwell, kind);

    private static List<RouteStopDraft> ValidStops() =>
        [Stop(1, Guid.NewGuid(), 0), Stop(2, Guid.NewGuid()), Stop(3, Guid.NewGuid())];

    [Fact]
    public void A_valid_route_is_created()
    {
        var result = Route.Create("MAIN", "Central to Riverside", ValidStops());
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Stops.Count);
    }

    [Fact]
    public void A_route_needs_two_stops()
    {
        var result = Route.Create("MAIN", "Central to Riverside", [Stop(1, Guid.NewGuid(), 0)]);
        Assert.Equal("Network.RouteNeedsTwoStops", result.Error.Code);
    }

    [Fact]
    public void A_gap_in_the_sequence_is_rejected()
    {
        var stops = new List<RouteStopDraft> { Stop(1, Guid.NewGuid(), 0), Stop(3, Guid.NewGuid()) };
        Assert.Equal("Network.RouteSequenceInvalid", Route.Create("MAIN", "Name", stops).Error.Code);
    }

    [Fact]
    public void A_repeated_station_is_rejected()
    {
        var station = Guid.NewGuid();
        var stops = new List<RouteStopDraft> { Stop(1, station, 0), Stop(2, Guid.NewGuid()), Stop(3, station) };
        Assert.Equal("Network.RouteStationRepeated", Route.Create("MAIN", "Name", stops).Error.Code);
    }

    [Fact]
    public void Negative_times_are_rejected()
    {
        var stops = new List<RouteStopDraft> { Stop(1, Guid.NewGuid(), 0), Stop(2, Guid.NewGuid(), -1) };
        Assert.Equal("Network.RouteTimeNegative", Route.Create("MAIN", "Name", stops).Error.Code);
    }

    [Fact]
    public void The_first_stop_has_no_time_from_a_previous_stop()
    {
        var stops = new List<RouteStopDraft> { Stop(1, Guid.NewGuid(), 4), Stop(2, Guid.NewGuid()) };
        Assert.Equal("Network.RouteFirstStopTime", Route.Create("MAIN", "Name", stops).Error.Code);
    }

    [Theory]
    [InlineData("main")]
    [InlineData("M")]
    [InlineData("TOOLONGROUTE")]
    public void Invalid_route_codes_are_rejected(string code)
    {
        Assert.Equal("Network.InvalidRouteCode", Route.Create(code, "Name", ValidStops()).Error.Code);
    }
}
