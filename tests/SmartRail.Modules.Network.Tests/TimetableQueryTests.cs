using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Application.Timetables.GetService;
using SmartRail.Modules.Network.Application.Timetables.GetStationTimetable;

namespace SmartRail.Modules.Network.Tests;

public sealed class TimetableQueryTests
{
    private static readonly Dictionary<string, string> None = new();
    private static readonly Guid ExpressId = Guid.NewGuid();
    private static readonly Guid CoastId = Guid.NewGuid();

    private static ServiceStopReadModel Stop(int seq, string code, int? arrive, int? leave, string kind = "Stop", string? platform = "1") =>
        new(seq, code, code + " name", None, arrive, leave, platform, kind);

    private static TimetableDayReadModel Day() => new(Guid.NewGuid(),
    [
        new ServiceReadModel(ExpressId, "1001", "MAIN",
        [
            Stop(1, "CEN", null, 480), Stop(2, "OAK", 486, 487), Stop(3, "RIV", 495, null)
        ]),
        new ServiceReadModel(CoastId, "3001", "COAST",
        [
            Stop(1, "CEN", null, 1430),                              // 23:50
            Stop(2, "HAR", 1438, 1438, "Passing", null),
            Stop(3, "LAK", 1447, 1448),
            Stop(4, "BAY", 1465, null)                               // 00:25 the next day
        ]),
        new ServiceReadModel(Guid.NewGuid(), "1501", "MAIN",
        [
            Stop(1, "RIV", null, 500), Stop(2, "OAK", 508, 509, "SetDownOnly"), Stop(3, "CEN", 517, null)
        ])
    ]);

    private sealed class FakeTimetable(TimetableDayReadModel? day) : ITimetableReader
    {
        public Task<TimetableDayReadModel?> GetDayAsync(DateOnly date, CancellationToken ct) => Task.FromResult(day);

        public Task<bool> ServiceExistsAsync(Guid serviceId, CancellationToken ct) =>
            Task.FromResult(day is not null && day.Services.Any(s => s.ServiceId == serviceId));
    }

    private sealed class FakeNetwork : INetworkReader
    {
        public Task<IReadOnlyList<LanguageReadModel>> GetLanguagesAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<LanguageReadModel>>([new("en", true), new("si", false)]);

        public Task<IReadOnlyList<StationReadModel>> GetActiveStationsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<StationReadModel>>([]);

        public Task<StationReadModel?> GetActiveStationAsync(string code, CancellationToken ct) =>
            Task.FromResult<StationReadModel?>(code is "CEN" or "OAK" or "BAY"
                ? new StationReadModel(code, code + " name", None, [], [])
                : null);

        public Task<IReadOnlyList<RouteReadModel>> GetActiveRoutesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<RouteReadModel>>([]);

        public Task<RouteReadModel?> GetActiveRouteAsync(string code, CancellationToken ct) => Task.FromResult<RouteReadModel?>(null);
    }

    private sealed class FakeSettings : INetworkSettings
    {
        public string TimeZoneId => "Asia/Colombo";

        public DateOnly Today() => new(2026, 10, 14);
    }

    private static GetStationTimetableQueryHandler StationHandler(TimetableDayReadModel? day) =>
        new(new FakeNetwork(), new FakeTimetable(day), new FakeSettings());

    [Fact]
    public async Task Departures_are_sorted_and_list_the_destination()
    {
        var result = (await StationHandler(Day()).Handle(new GetStationTimetableQuery("CEN", "2026-10-14", null, null), default)).Value;

        Assert.Equal("departures", result.Kind);
        Assert.Equal(["1001", "3001"], result.Items.Select(i => i.TrainNumber));
        Assert.Equal("08:00", result.Items[0].ScheduledTime);
        Assert.Equal("RIV", result.Items[0].OtherStationCode);
        Assert.Equal("BAY", result.Items[1].OtherStationCode);   // the passing point is not the destination
    }

    [Fact]
    public async Task A_service_that_only_ends_here_is_only_in_arrivals()
    {
        var handler = StationHandler(Day());
        var departures = (await handler.Handle(new GetStationTimetableQuery("CEN", "2026-10-14", "departures", null), default)).Value;
        var arrivals = (await handler.Handle(new GetStationTimetableQuery("CEN", "2026-10-14", "ARRIVALS", null), default)).Value;

        Assert.DoesNotContain(departures.Items, i => i.TrainNumber == "1501");
        Assert.Equal(["1501"], arrivals.Items.Select(i => i.TrainNumber));
        Assert.Equal("RIV", arrivals.Items[0].OtherStationCode);   // the origin
    }

    [Fact]
    public async Task A_set_down_only_stop_is_not_a_departure_and_a_passing_point_is_never_listed()
    {
        var handler = StationHandler(Day());

        var oak = (await handler.Handle(new GetStationTimetableQuery("OAK", "2026-10-14", "departures", null), default)).Value;
        Assert.Equal(["1001"], oak.Items.Select(i => i.TrainNumber));            // not 1501, which only sets down at OAK

        var oakArrivals = (await handler.Handle(new GetStationTimetableQuery("OAK", "2026-10-14", "arrivals", null), default)).Value;
        Assert.Equal(["1001", "1501"], oakArrivals.Items.Select(i => i.TrainNumber));
    }

    [Fact]
    public async Task Times_after_midnight_continue_past_24()
    {
        var arrivals = (await StationHandler(Day()).Handle(new GetStationTimetableQuery("BAY", "2026-10-14", "arrivals", null), default)).Value;
        Assert.Equal("24:25", arrivals.Items.Single().ScheduledTime);
        Assert.Equal(1465, arrivals.Items.Single().Minutes);
    }

    [Fact]
    public async Task A_missing_date_means_today_in_the_network_time_zone()
    {
        var result = (await StationHandler(Day()).Handle(new GetStationTimetableQuery("CEN", null, null, null), default)).Value;
        Assert.Equal("2026-10-14", result.Date);
        Assert.Equal("Asia/Colombo", result.TimeZone);
    }

    [Fact]
    public async Task No_timetable_and_unknown_station_are_not_found()
    {
        var none = await StationHandler(null).Handle(new GetStationTimetableQuery("CEN", "2025-01-01", null, null), default);
        Assert.Equal("Network.NoTimetableForDate", none.Error.Code);

        var unknown = await StationHandler(Day()).Handle(new GetStationTimetableQuery("ZZZ", "2026-10-14", null, null), default);
        Assert.Equal("Network.StationNotFound", unknown.Error.Code);
    }

    [Fact]
    public void The_date_and_kind_are_validated()
    {
        var validator = new GetStationTimetableQueryValidator();
        Assert.True(validator.Validate(new GetStationTimetableQuery("CEN", null, null, null)).IsValid);
        Assert.True(validator.Validate(new GetStationTimetableQuery("CEN", "2026-10-14", "Arrivals", null)).IsValid);
        Assert.False(validator.Validate(new GetStationTimetableQuery("CEN", "14/10/2026", null, null)).IsValid);
        Assert.False(validator.Validate(new GetStationTimetableQuery("CEN", "2026-02-30", null, null)).IsValid);
        Assert.False(validator.Validate(new GetStationTimetableQuery("CEN", null, "both", null)).IsValid);
    }

    [Fact]
    public async Task Service_detail_hides_passing_points_and_renumbers()
    {
        var handler = new GetServiceQueryHandler(new FakeNetwork(), new FakeTimetable(Day()), new FakeSettings());
        var result = (await handler.Handle(new GetServiceQuery(CoastId, "2026-10-14", null), default)).Value;

        Assert.Equal(["CEN", "LAK", "BAY"], result.Stops.Select(s => s.StationCode));
        Assert.Equal([1, 2, 3], result.Stops.Select(s => s.Sequence));
        Assert.Equal("23:50", result.Stops[0].ScheduledDeparture);
        Assert.Null(result.Stops[0].ScheduledArrival);
        Assert.Equal("24:25", result.Stops[2].ScheduledArrival);
        Assert.Null(result.Stops[2].ScheduledDeparture);
    }

    [Fact]
    public async Task Service_detail_distinguishes_unknown_from_not_running_today()
    {
        var running = new GetServiceQueryHandler(new FakeNetwork(), new FakeTimetable(Day()), new FakeSettings());
        var unknown = await running.Handle(new GetServiceQuery(Guid.NewGuid(), "2026-10-14", null), default);
        Assert.Equal("Network.ServiceNotFound", unknown.Error.Code);

        // The service exists but the timetable for that date does not contain it.
        var emptyDay = new TimetableDayReadModel(Guid.NewGuid(), []);
        var reader = new FakeTimetableWithKnownService(emptyDay, CoastId);
        var notRunning = await new GetServiceQueryHandler(new FakeNetwork(), reader, new FakeSettings())
            .Handle(new GetServiceQuery(CoastId, "2026-10-17", null), default);
        Assert.Equal("Network.ServiceNotRunning", notRunning.Error.Code);
    }

    private sealed class FakeTimetableWithKnownService(TimetableDayReadModel day, Guid knownId) : ITimetableReader
    {
        public Task<TimetableDayReadModel?> GetDayAsync(DateOnly date, CancellationToken ct) => Task.FromResult<TimetableDayReadModel?>(day);

        public Task<bool> ServiceExistsAsync(Guid serviceId, CancellationToken ct) => Task.FromResult(serviceId == knownId);
    }
}
