using SmartRail.Modules.Journeys.Domain.Journeys;

namespace SmartRail.Modules.Journeys.Tests;

internal static class Sample
{
    internal static readonly DateTimeOffset Departure = new(2026, 10, 14, 2, 0, 0, TimeSpan.Zero);

    internal static List<JourneyStopDraft> Stops() =>
    [
        new(1, "CEN", JourneyStopKind.Stop, null, Departure, 0, "1"),
        new(2, "MID", JourneyStopKind.Stop, Departure.AddMinutes(30), Departure.AddMinutes(32), 2, "2"),
        new(3, "END", JourneyStopKind.Stop, Departure.AddMinutes(60), null, 0, "1")
    ];

    internal static Journey NewJourney() =>
        Journey.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 10, 14), "1001", "MAIN", JourneyCreationOrigin.Generated, null, Stops()).Value;
}

public sealed class JourneyLifecycleRulesTests
{
    private static readonly Dictionary<JourneyLifecycle, JourneyLifecycle[]> Expected = new()
    {
        [JourneyLifecycle.Planned] = [JourneyLifecycle.Ready, JourneyLifecycle.Cancelled, JourneyLifecycle.TerminatedEmergency],
        [JourneyLifecycle.Ready] = [JourneyLifecycle.Planned, JourneyLifecycle.Running, JourneyLifecycle.Cancelled, JourneyLifecycle.TerminatedEmergency],
        [JourneyLifecycle.Running] = [JourneyLifecycle.Completed, JourneyLifecycle.TerminatedShort, JourneyLifecycle.Interrupted, JourneyLifecycle.TerminatedEmergency],
        [JourneyLifecycle.Interrupted] = [JourneyLifecycle.Running, JourneyLifecycle.TerminatedShort, JourneyLifecycle.Cancelled, JourneyLifecycle.TerminatedEmergency],
        [JourneyLifecycle.Cancelled] = [JourneyLifecycle.Planned],
        [JourneyLifecycle.Completed] = [],
        [JourneyLifecycle.TerminatedShort] = [],
        [JourneyLifecycle.TerminatedEmergency] = []
    };

    public static TheoryData<JourneyLifecycle, JourneyLifecycle> AllPairs()
    {
        var data = new TheoryData<JourneyLifecycle, JourneyLifecycle>();
        foreach (var from in Enum.GetValues<JourneyLifecycle>())
        foreach (var to in Enum.GetValues<JourneyLifecycle>())
            data.Add(from, to);
        return data;
    }

    // All 64 pairs match the state model exactly.
    [Theory]
    [MemberData(nameof(AllPairs))]
    public void Only_the_modelled_transitions_are_allowed(JourneyLifecycle from, JourneyLifecycle to) =>
        Assert.Equal(Expected[from].Contains(to), JourneyLifecycleRules.IsAllowed(from, to));

    [Fact]
    public void Ready_and_planned_both_show_as_planned_to_the_public() =>
        Assert.Equal(JourneyLifecycleRules.PublicStatus(JourneyLifecycle.Planned), JourneyLifecycleRules.PublicStatus(JourneyLifecycle.Ready));
}

public sealed class JourneyTests
{
    private static readonly Guid Vehicle = Guid.NewGuid();
    private static readonly Guid Duty = Guid.NewGuid();

    private static string CreateError(string number = "1001", string? reason = null, JourneyCreationOrigin origin = JourneyCreationOrigin.Generated, List<JourneyStopDraft>? stops = null) =>
        Journey.Create(null, null, new DateOnly(2026, 10, 14), number, "MAIN", origin, reason, stops ?? Sample.Stops()).Error.Code;

    [Fact]
    public void A_new_journey_is_planned_with_scheduled_confidence_and_its_ends_taken_from_the_stops()
    {
        var journey = Sample.NewJourney();

        Assert.Equal(JourneyLifecycle.Planned, journey.Lifecycle);
        Assert.Equal(JourneyConfidence.Scheduled, journey.Confidence);
        Assert.Equal("CEN", journey.OriginStationCode);
        Assert.Equal("END", journey.DestinationStationCode);
        Assert.Equal(Sample.Departure, journey.ScheduledDepartureUtc);
        Assert.Equal(Sample.Departure.AddMinutes(60), journey.ScheduledArrivalUtc);
    }

    [Fact]
    public void A_manual_journey_needs_a_reason() =>
        Assert.Equal("Journeys.ReasonRequired", CreateError(origin: JourneyCreationOrigin.Manual, reason: " "));

    [Theory]
    [InlineData("")]
    [InlineData("TOO-LONG-NUMBER")]
    [InlineData("12 3")]
    public void A_train_number_is_one_to_ten_letters_or_digits(string number) =>
        Assert.Equal("Journeys.InvalidTrainNumber", CreateError(number));

    [Fact]
    public void Stop_times_cannot_go_backwards()
    {
        var stops = Sample.Stops();
        stops[2] = stops[2] with { ScheduledArrivalUtc = Sample.Departure.AddMinutes(10) };

        Assert.Equal("Journeys.StopTimesInvalid", CreateError(stops: stops));
    }

    [Fact]
    public void The_first_and_last_stop_cannot_be_passing_points()
    {
        var stops = Sample.Stops();
        stops[0] = stops[0] with { Kind = JourneyStopKind.Passing };

        Assert.Equal("Journeys.PassingEndpoint", CreateError(stops: stops));
    }

    // Ready is derived from a vehicle plus a driver.
    [Fact]
    public void A_vehicle_alone_does_not_make_a_journey_ready_but_a_vehicle_and_a_driver_do()
    {
        var journey = Sample.NewJourney();

        Assert.True(journey.AssignVehicle(Vehicle, journey.ScheduledDepartureUtc, journey.ScheduledArrivalUtc, "t").IsSuccess);
        Assert.Equal(JourneyLifecycle.Planned, journey.Lifecycle);

        Assert.True(journey.AssignDriver(Duty, journey.ScheduledDepartureUtc.AddHours(-1), journey.ScheduledArrivalUtc, "t").IsSuccess);
        Assert.Equal(JourneyLifecycle.Ready, journey.Lifecycle);

        Assert.True(journey.UnassignDriver(journey.ScheduledDepartureUtc.AddMinutes(-30)).IsSuccess);
        Assert.Equal(JourneyLifecycle.Planned, journey.Lifecycle);
    }

    [Fact]
    public void A_second_vehicle_in_the_same_period_is_rejected_but_a_substitution_is_kept_in_history()
    {
        var journey = Sample.NewJourney();
        journey.AssignVehicle(Vehicle, journey.ScheduledDepartureUtc, journey.ScheduledArrivalUtc, "t");

        Assert.Equal("Journeys.AssignmentOverlaps",
            journey.AssignVehicle(Guid.NewGuid(), journey.ScheduledDepartureUtc, journey.ScheduledArrivalUtc, "t").Error.Code);

        Assert.True(journey.ReplaceVehicle(Guid.NewGuid(), journey.ScheduledDepartureUtc.AddMinutes(20), "t", "fault").IsSuccess);
        Assert.Equal(2, journey.VehicleAssignments.Count);
    }

    [Fact]
    public void A_cancelled_journey_can_be_reinstated_only_before_its_departure_and_with_a_reason()
    {
        var journey = Sample.NewJourney();
        Assert.True(journey.Cancel("STAFF").IsSuccess);
        Assert.Equal(JourneyLifecycle.Cancelled, journey.Lifecycle);

        Assert.Equal("Journeys.ReasonRequired", journey.Reinstate(Sample.Departure.AddHours(-1), " ").Error.Code);
        Assert.Equal("Journeys.ReinstateTooLate", journey.Reinstate(Sample.Departure, "ok").Error.Code);

        Assert.True(journey.Reinstate(Sample.Departure.AddHours(-1), "ok").IsSuccess);
        Assert.Equal(JourneyLifecycle.Planned, journey.Lifecycle);
        Assert.Null(journey.CancelReasonCode);
    }

    [Fact]
    public void The_scheduled_times_do_not_change_when_the_lifecycle_does()
    {
        var journey = Sample.NewJourney();
        var departure = journey.ScheduledDepartureUtc;
        var stopTimes = journey.Stops.Select(s => (s.ScheduledArrivalUtc, s.ScheduledDepartureUtc)).ToList();

        journey.Cancel("STAFF");
        journey.Reinstate(Sample.Departure.AddHours(-1), "ok");

        Assert.Equal(departure, journey.ScheduledDepartureUtc);
        Assert.Equal(stopTimes, journey.Stops.Select(s => (s.ScheduledArrivalUtc, s.ScheduledDepartureUtc)).ToList());
    }

    [Fact]
    public void An_illegal_transition_is_a_conflict() =>
        Assert.Equal("Journeys.IllegalTransition", Sample.NewJourney().TransitionTo(JourneyLifecycle.Completed).Error.Code);
}
