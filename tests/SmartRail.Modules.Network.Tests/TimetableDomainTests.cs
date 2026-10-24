using SmartRail.Modules.Network.Domain.Routes;
using SmartRail.Modules.Network.Domain.Timetables;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Tests;

public sealed class ServiceCalendarTests
{
    private static ServiceCalendar Weekdays() =>
        ServiceCalendar.Create(
            "WEEKDAYS", "Monday to Friday",
            new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            new DateOnly(2026, 1, 1), null).Value;

    [Fact]
    public void Runs_on_its_weekdays_and_not_at_weekends()
    {
        var calendar = Weekdays();
        Assert.True(calendar.RunsOn(new DateOnly(2026, 10, 14)));    // Wednesday
        Assert.False(calendar.RunsOn(new DateOnly(2026, 10, 17)));   // Saturday
    }

    [Fact]
    public void A_removed_exception_cancels_the_day()
    {
        var calendar = Weekdays();
        calendar.AddException(new DateOnly(2026, 10, 23), CalendarExceptionKind.Removed, "Holiday");
        Assert.False(calendar.RunsOn(new DateOnly(2026, 10, 23)));
        Assert.True(calendar.RunsOn(new DateOnly(2026, 10, 22)));
    }

    [Fact]
    public void An_added_exception_adds_a_day_that_does_not_normally_run()
    {
        var calendar = Weekdays();
        calendar.AddException(new DateOnly(2026, 10, 17), CalendarExceptionKind.Added, "Special");
        Assert.True(calendar.RunsOn(new DateOnly(2026, 10, 17)));
    }

    [Fact]
    public void It_does_not_run_outside_its_valid_range()
    {
        var calendar = ServiceCalendar.Create("SUMMER", "Summer",
            new HashSet<DayOfWeek> { DayOfWeek.Monday }, new DateOnly(2026, 6, 1), new DateOnly(2026, 8, 31)).Value;
        Assert.False(calendar.RunsOn(new DateOnly(2026, 5, 25)));    // a Monday before the range
        Assert.True(calendar.RunsOn(new DateOnly(2026, 6, 1)));
        Assert.False(calendar.RunsOn(new DateOnly(2026, 9, 7)));     // a Monday after the range
    }

    [Fact]
    public void Two_exceptions_on_one_date_are_rejected()
    {
        var calendar = Weekdays();
        Assert.True(calendar.AddException(new DateOnly(2026, 10, 23), CalendarExceptionKind.Removed, null).IsSuccess);
        Assert.Equal("Network.DuplicateCalendarException",
            calendar.AddException(new DateOnly(2026, 10, 23), CalendarExceptionKind.Added, null).Error.Code);
    }

    [Fact]
    public void A_calendar_needs_a_day_and_a_sensible_range()
    {
        Assert.Equal("Network.CalendarRangeInvalid",
            ServiceCalendar.Create("NONE", "None", new HashSet<DayOfWeek>(), new DateOnly(2026, 1, 1), null).Error.Code);
        Assert.Equal("Network.CalendarRangeInvalid",
            ServiceCalendar.Create("BAD", "Bad", new HashSet<DayOfWeek> { DayOfWeek.Monday }, new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1)).Error.Code);
    }
}

public sealed class VersionResolverTests
{
    private static TimetableVersionSummary Version(TimetableVersionStatus status, DateOnly? effective) =>
        new(Guid.NewGuid(), status, effective);

    [Fact]
    public void A_date_uses_the_latest_version_in_force_on_or_before_it()
    {
        var older = Version(TimetableVersionStatus.Superseded, new DateOnly(2026, 1, 1));
        var newer = Version(TimetableVersionStatus.Published, new DateOnly(2026, 11, 1));
        var all = new[] { older, newer };

        Assert.Equal(older.Id, VersionResolver.Resolve(all, new DateOnly(2026, 10, 31))!.Id);
        Assert.Equal(newer.Id, VersionResolver.Resolve(all, new DateOnly(2026, 11, 1))!.Id);
        Assert.Equal(newer.Id, VersionResolver.Resolve(all, new DateOnly(2027, 3, 1))!.Id);
    }

    [Fact]
    public void Drafts_and_dates_before_any_version_resolve_to_nothing()
    {
        var draft = Version(TimetableVersionStatus.Draft, null);
        var published = Version(TimetableVersionStatus.Published, new DateOnly(2026, 1, 1));

        Assert.Null(VersionResolver.Resolve(new[] { draft }, new DateOnly(2026, 10, 1)));
        Assert.Null(VersionResolver.Resolve(new[] { published }, new DateOnly(2025, 12, 31)));
    }
}

public sealed class ServiceTests
{
    private static readonly Guid A = Guid.NewGuid(), B = Guid.NewGuid(), C = Guid.NewGuid(), D = Guid.NewGuid();

    private static Route MainRoute() => Route.Create("MAIN", "Main",
    [
        new RouteStopDraft(1, A, null, 0, 1, StopKind.Stop),
        new RouteStopDraft(2, B, null, 5, 1, StopKind.Stop),
        new RouteStopDraft(3, C, null, 5, 1, StopKind.Stop),
        new RouteStopDraft(4, D, null, 5, 1, StopKind.Stop)
    ]).Value;

    private static ServiceStopDraft Stop(int seq, Guid station, int? arrive, int? leave, StopKind kind = StopKind.Stop) =>
        new(seq, station, arrive, leave, null, kind);

    private static List<ServiceStopDraft> Valid() =>
    [
        Stop(1, A, null, 480),
        Stop(2, B, 486, 487),
        Stop(3, D, 495, null)                // skips C: an express
    ];

    private static Result<Service> Make(IReadOnlyList<ServiceStopDraft> stops, ServiceDirection direction = ServiceDirection.Forward) =>
        Service.Create(Guid.NewGuid(), MainRoute(), Guid.NewGuid(), "1001", direction, ServiceCategory.Regional, stops);

    [Fact]
    public void An_express_may_skip_stations()
    {
        Assert.True(Make(Valid()).IsSuccess);
    }

    [Fact]
    public void A_reverse_service_follows_the_route_backwards()
    {
        var stops = new List<ServiceStopDraft> { Stop(1, D, null, 480), Stop(2, C, 486, 487), Stop(3, A, 500, null) };
        Assert.True(Make(stops, ServiceDirection.Reverse).IsSuccess);
        Assert.Equal("Network.ServiceStopsNotOnRoute", Make(stops, ServiceDirection.Forward).Error.Code);
    }

    [Fact]
    public void Stops_in_the_wrong_order_are_rejected()
    {
        var stops = new List<ServiceStopDraft> { Stop(1, B, null, 480), Stop(2, A, 486, null) };
        Assert.Equal("Network.ServiceStopsNotOnRoute", Make(stops).Error.Code);
    }

    [Fact]
    public void A_station_that_is_not_on_the_route_is_rejected()
    {
        var stops = new List<ServiceStopDraft> { Stop(1, A, null, 480), Stop(2, Guid.NewGuid(), 486, null) };
        Assert.Equal("Network.ServiceStopsNotOnRoute", Make(stops).Error.Code);
    }

    [Fact]
    public void Fewer_than_two_stops_a_gap_or_a_repeat_are_rejected()
    {
        Assert.Equal("Network.ServiceNeedsTwoStops", Make([Stop(1, A, null, 480)]).Error.Code);
        Assert.Equal("Network.ServiceSequenceInvalid", Make([Stop(1, A, null, 480), Stop(3, B, 486, null)]).Error.Code);
        Assert.Equal("Network.ServiceStationRepeated", Make([Stop(1, A, null, 480), Stop(2, A, 486, null)]).Error.Code);
    }

    [Fact]
    public void Times_that_go_backwards_or_leave_the_range_are_rejected()
    {
        Assert.Equal("Network.ServiceTimesInvalid",
            Make([Stop(1, A, null, 480), Stop(2, B, 470, 471), Stop(3, C, 500, null)]).Error.Code);
        Assert.Equal("Network.ServiceTimeOutOfRange",
            Make([Stop(1, A, null, 480), Stop(2, B, 3000, null)]).Error.Code);
        Assert.Equal("Network.ServiceTimesInvalid",
            Make([Stop(1, A, null, 480), Stop(2, B, 490, 485)]).Error.Code);   // departs before it arrives
    }

    [Fact]
    public void The_first_stop_has_no_arrival_and_the_last_no_departure()
    {
        Assert.Equal("Network.ServiceTimesInvalid", Make([Stop(1, A, 470, 480), Stop(2, B, 486, null)]).Error.Code);
        Assert.Equal("Network.ServiceTimesInvalid", Make([Stop(1, A, null, 480), Stop(2, B, 486, 490)]).Error.Code);
    }

    [Fact]
    public void Times_may_pass_midnight()
    {
        var stops = new List<ServiceStopDraft> { Stop(1, A, null, 1430), Stop(2, B, 1436, 1437), Stop(3, C, 1465, null) };
        Assert.True(Make(stops).IsSuccess);
    }

    [Fact]
    public void A_passing_point_cannot_be_the_first_or_last_stop()
    {
        var stops = new List<ServiceStopDraft> { Stop(1, A, null, 480, StopKind.Passing), Stop(2, B, 486, null) };
        Assert.Equal("Network.ServiceEndpointPassing", Make(stops).Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("TOO-LONG-NUMBER")]
    [InlineData("12 3")]
    public void An_invalid_train_number_is_rejected(string number)
    {
        var result = Service.Create(Guid.NewGuid(), MainRoute(), Guid.NewGuid(), number, ServiceDirection.Forward, ServiceCategory.Regional, Valid());
        Assert.Equal("Network.InvalidTrainNumber", result.Error.Code);
    }
}

public sealed class TimetableVersionTests
{
    private static Route NewRoute()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        return Route.Create("MAIN", "Main",
        [
            new RouteStopDraft(1, a, null, 0, 1, StopKind.Stop),
            new RouteStopDraft(2, b, null, 5, 1, StopKind.Stop)
        ]).Value;
    }

    private static (TimetableVersion Version, Service Service, Route Route) VersionWithService()
    {
        var version = TimetableVersion.CreateDraft("Autumn", "alice").Value;
        var route = NewRoute();
        var stations = route.Stops.Select(s => s.StationId).ToList();
        var service = Service.Create(version.Id, route, Guid.NewGuid(), "1001", ServiceDirection.Forward, ServiceCategory.Regional,
        [
            new ServiceStopDraft(1, stations[0], null, 480, null, StopKind.Stop),
            new ServiceStopDraft(2, stations[1], 486, null, null, StopKind.Stop)
        ]).Value;
        return (version, service, route);
    }

    [Fact]
    public void A_version_goes_draft_review_published_superseded()
    {
        var (version, service, _) = VersionWithService();
        Assert.True(version.AddService(service).IsSuccess);
        Assert.True(version.SubmitForReview().IsSuccess);
        Assert.True(version.Publish(new DateOnly(2026, 11, 1), "bob", DateTimeOffset.UtcNow).IsSuccess);
        Assert.Equal(TimetableVersionStatus.Published, version.Status);
        Assert.True(version.Supersede().IsSuccess);
        Assert.Equal(TimetableVersionStatus.Superseded, version.Status);
    }

    [Fact]
    public void The_author_can_approve_their_own_version()
    {
        var (version, service, _) = VersionWithService();
        version.AddService(service);
        version.SubmitForReview();

        var result = version.Publish(new DateOnly(2026, 11, 1), "ALICE", DateTimeOffset.UtcNow);
        Assert.True(result.IsSuccess);
        Assert.Equal(TimetableVersionStatus.Published, version.Status);
    }

    [Fact]
    public void A_published_version_cannot_be_changed()
    {
        var (version, service, route) = VersionWithService();
        version.AddService(service);
        version.SubmitForReview();
        version.Publish(new DateOnly(2026, 11, 1), "bob", DateTimeOffset.UtcNow);

        var drafts = service.Stops
            .Select(st => new ServiceStopDraft(st.Sequence, st.StationId, st.ArrivalMinutes, st.DepartureMinutes, null, st.Kind))
            .ToList();
        var second = Service.Create(version.Id, route, Guid.NewGuid(), "1002", ServiceDirection.Forward, ServiceCategory.Regional, drafts).Value;
        var result = version.AddService(second);

        Assert.Equal("Network.VersionNotEditable", result.Error.Code);
    }

    [Fact]
    public void An_empty_version_cannot_be_submitted_and_publishing_needs_review_first()
    {
        var draft = TimetableVersion.CreateDraft("Empty", "alice").Value;
        Assert.Equal("Network.VersionHasNoServices", draft.SubmitForReview().Error.Code);
        Assert.Equal("Network.VersionWrongState", draft.Publish(new DateOnly(2026, 11, 1), "bob", DateTimeOffset.UtcNow).Error.Code);
    }

    [Fact]
    public void Times_of_a_service_can_change_in_a_draft_and_stay_valid()
    {
        var (version, service, _) = VersionWithService();
        version.AddService(service);

        Assert.True(version.ChangeServiceTimes(service.Id, [new ServiceStopTime(null, 490), new ServiceStopTime(497, null)]).IsSuccess);
        Assert.Equal(490, service.Stops.First(s => s.Sequence == 1).DepartureMinutes);
        Assert.Equal(497, service.Stops.First(s => s.Sequence == 2).ArrivalMinutes);

        // going backwards or a wrong number of stops is refused and nothing changes
        Assert.True(version.ChangeServiceTimes(service.Id, [new ServiceStopTime(null, 500), new ServiceStopTime(400, null)]).IsFailure);
        Assert.True(version.ChangeServiceTimes(service.Id, [new ServiceStopTime(null, 500)]).IsFailure);
        Assert.Equal(490, service.Stops.First(s => s.Sequence == 1).DepartureMinutes);
    }

    [Fact]
    public void A_service_can_be_removed_from_a_draft_but_not_from_a_published_version()
    {
        var (version, service, _) = VersionWithService();
        version.AddService(service);
        version.SubmitForReview();
        version.Publish(new DateOnly(2026, 11, 1), "bob", DateTimeOffset.UtcNow);

        Assert.Equal("Network.VersionNotEditable", version.RemoveService(service.Id).Error.Code);
        Assert.Equal("Network.VersionNotEditable", version.ChangeServiceTimes(service.Id, [new ServiceStopTime(null, 490), new ServiceStopTime(497, null)]).Error.Code);

        var (draft, other, _) = VersionWithService();
        draft.AddService(other);
        Assert.True(draft.RemoveService(other.Id).IsSuccess);
        Assert.Empty(draft.Services);
        Assert.Equal("Network.ServiceNotInVersion", draft.RemoveService(other.Id).Error.Code);
    }
}
