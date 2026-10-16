using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Application.Routes.GetRoute;
using SmartRail.Modules.Network.Application.Routes.GetRoutes;
using SmartRail.Modules.Network.Application.Stations.GetStation;
using SmartRail.Modules.Network.Application.Stations.GetStations;

namespace SmartRail.Modules.Network.Tests;

public sealed class QueryHandlerTests
{
    private static readonly Dictionary<string, string> NoTranslations = new();

    private static StationReadModel Station(string code, string name, string? sinhala = null) => new(
        code,
        name,
        sinhala is null ? NoTranslations : new Dictionary<string, string> { ["si"] = sinhala },
        [],
        [new PlatformReadModel("1", true)]);

    private static RouteStopReadModel Stop(int seq, string code, int minutes, int dwell, string kind = "Stop") =>
        new(seq, code, code, NoTranslations, null, minutes, dwell, kind);

    private sealed class FakeReader(
        IReadOnlyList<StationReadModel>? stations = null,
        IReadOnlyList<RouteReadModel>? routes = null) : INetworkReader
    {
        private readonly IReadOnlyList<StationReadModel> _stations = stations ?? [];
        private readonly IReadOnlyList<RouteReadModel> _routes = routes ?? [];

        public Task<IReadOnlyList<LanguageReadModel>> GetLanguagesAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<LanguageReadModel>>([new("en", true), new("si", false)]);

        public Task<IReadOnlyList<StationReadModel>> GetActiveStationsAsync(CancellationToken ct) => Task.FromResult(_stations);

        public Task<StationReadModel?> GetActiveStationAsync(string code, CancellationToken ct) =>
            Task.FromResult(_stations.FirstOrDefault(s => s.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<RouteReadModel>> GetActiveRoutesAsync(CancellationToken ct) => Task.FromResult(_routes);

        public Task<RouteReadModel?> GetActiveRouteAsync(string code, CancellationToken ct) =>
            Task.FromResult(_routes.FirstOrDefault(r => r.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Stations_are_sorted_and_searchable_in_any_case()
    {
        var reader = new FakeReader([Station("RIV", "Riverside"), Station("OAK", "Oak Park"), Station("CEN", "Central")]);
        var handler = new GetStationsQueryHandler(reader);

        var all = (await handler.Handle(new GetStationsQuery(null, null), default)).Value;
        Assert.Equal(["Central", "Oak Park", "Riverside"], all.Items.Select(i => i.Name));

        var filtered = (await handler.Handle(new GetStationsQuery("OAK", null), default)).Value;
        Assert.Equal(["OAK"], filtered.Items.Select(i => i.Code));
    }

    [Fact]
    public async Task Unknown_language_falls_back_to_the_default()
    {
        var reader = new FakeReader([Station("CEN", "Central", "මධ්‍යම")]);
        var handler = new GetStationsQueryHandler(reader);

        var sinhala = (await handler.Handle(new GetStationsQuery(null, "si"), default)).Value;
        Assert.Equal("si", sinhala.Language);
        Assert.Equal("මධ්‍යම", sinhala.Items[0].Name);

        var unknown = (await handler.Handle(new GetStationsQuery(null, "xx"), default)).Value;
        Assert.Equal("en", unknown.Language);
        Assert.Equal("Central", unknown.Items[0].Name);
    }

    [Fact]
    public void A_one_character_search_is_invalid()
    {
        var validator = new GetStationsQueryValidator();
        Assert.False(validator.Validate(new GetStationsQuery("o", null)).IsValid);
        Assert.True(validator.Validate(new GetStationsQuery("oa", null)).IsValid);
        Assert.True(validator.Validate(new GetStationsQuery(null, null)).IsValid);
    }

    [Fact]
    public async Task Unknown_station_is_not_found()
    {
        var handler = new GetStationQueryHandler(new FakeReader());
        var result = await handler.Handle(new GetStationQuery("ZZZ", null), default);
        Assert.Equal("Network.StationNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Passing_points_are_hidden_and_the_sequence_is_renumbered()
    {
        var route = new RouteReadModel("COAST", "Central to Bay View",
        [
            Stop(1, "CEN", 0, 2),
            Stop(2, "HAR", 8, 0, "Passing"),
            Stop(3, "LAK", 9, 1),
            Stop(4, "BAY", 7, 2)
        ]);
        var reader = new FakeReader(routes: [route]);

        var response = (await new GetRouteQueryHandler(reader).Handle(new GetRouteQuery("coast", null), default)).Value;

        Assert.Equal(["CEN", "LAK", "BAY"], response.Stops.Select(s => s.StationCode));
        Assert.Equal([1, 2, 3], response.Stops.Select(s => s.Sequence));
        // The passing point's 8 minutes are carried into the next visible stop (9 + 8).
        Assert.Equal(17, response.Stops[1].MinutesFromPrevious);

        var list = (await new GetRoutesQueryHandler(reader).Handle(new GetRoutesQuery(), default)).Value;
        Assert.Equal(3, list.Items[0].StopCount);
    }

    [Fact]
    public async Task Unknown_route_is_not_found()
    {
        var result = await new GetRouteQueryHandler(new FakeReader()).Handle(new GetRouteQuery("NOPE", null), default);
        Assert.Equal("Network.RouteNotFound", result.Error.Code);
    }
}
