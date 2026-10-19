using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;

namespace SmartRail.Modules.Journeys.Tests;

// SPEC-005: stop events, delay, punctuality, ETAs, early running, confidence and staleness.
public sealed class LiveJourneyTests
{
    private static readonly LiveRules Rules = LiveRules.Default;
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 2, 10, 0, TimeSpan.Zero);

    // CEN dep 02:00 | MID arr 02:30 dep 02:32 (dwell 2) | END arr 03:00
    private static Journey Ready()
    {
        var journey = Sample.NewJourney();
        journey.AssignVehicle(Guid.NewGuid(), journey.ScheduledDepartureUtc.AddHours(-1), journey.ScheduledArrivalUtc, "t");
        journey.AssignDriver(Guid.NewGuid(), journey.ScheduledDepartureUtc.AddHours(-1), journey.ScheduledArrivalUtc, "t");
        Assert.Equal(JourneyLifecycle.Ready, journey.Lifecycle);
        return journey;
    }

    private static StopEventOutcome Apply(Journey journey, StopEventType type, int stop, DateTimeOffset at) =>
        journey.ApplyStopEvent(type, stop, at, Now, Rules).Value;

    // AC-2
    [Fact]
    public void The_first_departure_makes_a_ready_journey_running()
    {
        var journey = Ready();

        var outcome = Apply(journey, StopEventType.Departed, 1, Sample.Departure);

        Assert.True(outcome.Departed);
        Assert.Equal(JourneyLifecycle.Running, journey.Lifecycle);
        Assert.Equal(RunningSubState.InSection, journey.RunningSubState);
        Assert.Equal(JourneyStopState.Departed, journey.Stops[0].State);
        Assert.Equal(Sample.Departure, journey.Stops[0].ActualDepartureUtc);
        Assert.Equal(JourneyConfidence.Live, journey.Confidence);
        Assert.Equal(Now, journey.LastAcceptedUpdateAt);
    }

    // AC-3: +8 minutes, MinorDelay, lifecycle unchanged, ETAs recovered by dwell, scheduled times untouched.
    [Fact]
    public void A_late_departure_gives_a_delay_a_band_and_downstream_estimates_without_touching_the_schedule()
    {
        var journey = Ready();
        var scheduled = journey.Stops.Select(s => (s.ScheduledArrivalUtc, s.ScheduledDepartureUtc)).ToList();

        Apply(journey, StopEventType.Departed, 1, Sample.Departure.AddMinutes(8));

        Assert.Equal(8, journey.DelayMinutes);
        Assert.Equal(PunctualityBand.MinorDelay, journey.PunctualityBand);
        Assert.Equal(JourneyLifecycle.Running, journey.Lifecycle);

        var mid = journey.Stops[1];
        Assert.Equal(Sample.Departure.AddMinutes(38), mid.EstimatedArrivalUtc);     // 02:30 + 8
        Assert.Equal(Sample.Departure.AddMinutes(39), mid.EstimatedDepartureUtc);   // dwell 2 recovers 1 minute (minimum dwell 1)
        Assert.Equal(EstimateSource.Live, mid.EstimateSource);

        var end = journey.Stops[2];
        Assert.Equal(Sample.Departure.AddMinutes(67), end.EstimatedArrivalUtc);     // 03:00 + 7 carried
        Assert.Equal(EstimateSource.Live, end.EstimateSource);

        Assert.Equal(scheduled, journey.Stops.Select(s => (s.ScheduledArrivalUtc, s.ScheduledDepartureUtc)).ToList());
    }

    // AC-4
    [Fact]
    public void Early_running_shows_an_early_arrival_but_never_an_earlier_departure()
    {
        var journey = Ready();
        Apply(journey, StopEventType.Departed, 1, Sample.Departure);

        Apply(journey, StopEventType.Arrived, 2, Sample.Departure.AddMinutes(26));   // 4 minutes before 02:30

        Assert.Equal(-4, journey.DelayMinutes);
        Assert.Equal(PunctualityBand.Early, journey.PunctualityBand);
        Assert.Equal(Sample.Departure.AddMinutes(32), journey.Stops[1].EstimatedDepartureUtc);   // scheduled, not 02:27
        Assert.Equal(Sample.Departure.AddMinutes(60), journey.Stops[2].EstimatedArrivalUtc);     // nothing carried forward
    }

    [Theory]
    [InlineData(-5, PunctualityBand.Early)]
    [InlineData(-2, PunctualityBand.Early)]
    [InlineData(-1, PunctualityBand.OnTime)]
    [InlineData(0, PunctualityBand.OnTime)]
    [InlineData(1, PunctualityBand.OnTime)]
    [InlineData(2, PunctualityBand.MinorDelay)]
    [InlineData(9, PunctualityBand.MinorDelay)]
    [InlineData(10, PunctualityBand.MajorDelay)]
    [InlineData(45, PunctualityBand.MajorDelay)]
    public void Punctuality_bands_follow_the_state_model(int delay, PunctualityBand expected) =>
        Assert.Equal(expected, LiveRules.BandFor(delay));

    [Fact]
    public void Delay_is_rounded_to_the_nearest_whole_minute()
    {
        var journey = Ready();
        Apply(journey, StopEventType.Departed, 1, Sample.Departure.AddSeconds(8 * 60 + 40));   // 8 min 40 s

        Assert.Equal(9, journey.DelayMinutes);
    }

    // AC-9
    [Fact]
    public void Arriving_at_the_last_stop_completes_the_journey_and_keeps_the_final_delay()
    {
        var journey = Ready();
        Apply(journey, StopEventType.Departed, 1, Sample.Departure);
        Apply(journey, StopEventType.Arrived, 2, Sample.Departure.AddMinutes(30));
        Apply(journey, StopEventType.Departed, 2, Sample.Departure.AddMinutes(32));

        var outcome = Apply(journey, StopEventType.Arrived, 3, Sample.Departure.AddMinutes(63));

        Assert.True(outcome.Completed);
        Assert.Equal(JourneyLifecycle.Completed, journey.Lifecycle);
        Assert.Equal(JourneyConfidence.Confirmed, journey.Confidence);
        Assert.Equal(3, journey.DelayMinutes);
        Assert.Null(journey.RunningSubState);
        Assert.All(journey.Stops, s => Assert.Equal(EstimateSource.Confirmed, s.EstimateSource));
    }

    // AC-10
    [Fact]
    public void A_later_confirmed_stop_resolves_earlier_pending_stops_without_inventing_times()
    {
        var journey = Ready();

        var outcome = Apply(journey, StopEventType.Arrived, 3, Sample.Departure.AddMinutes(61));

        Assert.Equal([1, 2], outcome.InferredStops);
        Assert.All(journey.Stops.Where(s => s.Sequence < 3), s =>
        {
            Assert.Equal(JourneyStopState.Departed, s.State);
            Assert.Null(s.ActualArrivalUtc);
            Assert.Null(s.ActualDepartureUtc);
        });
        Assert.Equal(1, journey.DelayMinutes);
    }

    // AC-11, AC-12
    [Fact]
    public void Events_are_refused_when_the_journey_cannot_take_them()
    {
        var planned = Sample.NewJourney();
        Assert.Equal("Journeys.JourneyNotReady", planned.ApplyStopEvent(StopEventType.Departed, 1, Sample.Departure, Now, Rules).Error.Code);

        var cancelled = Ready();
        cancelled.Cancel("STAFF");
        Assert.Equal("Journeys.JourneyEnded", cancelled.ApplyStopEvent(StopEventType.Departed, 1, Sample.Departure, Now, Rules).Error.Code);

        Assert.Equal("Journeys.StopNotFound", Ready().ApplyStopEvent(StopEventType.Departed, 9, Sample.Departure, Now, Rules).Error.Code);
    }

    // AC-19: one delay-changed event per real change, not per message.
    [Fact]
    public void A_delay_change_is_raised_once_and_not_again_for_the_same_delay()
    {
        var journey = Ready();

        Apply(journey, StopEventType.Departed, 1, Sample.Departure.AddMinutes(8));
        Apply(journey, StopEventType.Arrived, 2, Sample.Departure.AddMinutes(38));   // still +8

        Assert.Single(journey.DomainEvents.OfType<JourneyDelayChangedDomainEvent>());
        Assert.Single(journey.DomainEvents.OfType<JourneyDepartedDomainEvent>());
    }

    // The same facts in a different arrival order give the same picture.
    [Fact]
    public void The_result_depends_on_the_actual_times_not_on_the_order_the_messages_arrive_in()
    {
        var a = Ready();
        Apply(a, StopEventType.Departed, 1, Sample.Departure.AddMinutes(2));
        Apply(a, StopEventType.Arrived, 2, Sample.Departure.AddMinutes(33));

        var b = Ready();
        Apply(b, StopEventType.Arrived, 2, Sample.Departure.AddMinutes(33));
        Apply(b, StopEventType.Departed, 1, Sample.Departure.AddMinutes(2));

        Assert.Equal(a.DelayMinutes, b.DelayMinutes);
        Assert.Equal(a.Stops.Select(s => s.EstimatedDepartureUtc), b.Stops.Select(s => s.EstimatedDepartureUtc));
    }

    // AC-13, AC-14
    [Theory]
    [InlineData(0, JourneyConfidence.Live, false, true)]
    [InlineData(59, JourneyConfidence.Live, false, true)]
    [InlineData(60, JourneyConfidence.Live, true, true)]
    [InlineData(299, JourneyConfidence.Live, true, true)]
    [InlineData(300, JourneyConfidence.Unknown, false, false)]
    [InlineData(900, JourneyConfidence.Unknown, false, false)]
    public void A_running_journey_goes_stale_after_60_seconds_and_unknown_after_300(
        int ageSeconds, JourneyConfidence confidence, bool stale, bool showsLiveTimes)
    {
        var last = new DateTimeOffset(2026, 10, 14, 2, 10, 0, TimeSpan.Zero);

        var status = LiveStatus.Evaluate(JourneyLifecycle.Running, JourneyConfidence.Live, last, last.AddSeconds(ageSeconds), Rules);

        Assert.Equal(confidence, status.Confidence);
        Assert.Equal(stale, status.IsStale);
        Assert.Equal(showsLiveTimes, status.ShowLiveTimes);
    }

    [Fact]
    public void Confidence_for_journeys_that_are_not_running_follows_the_state_model()
    {
        var now = Now;

        Assert.Equal(JourneyConfidence.Scheduled, LiveStatus.Evaluate(JourneyLifecycle.Ready, JourneyConfidence.Scheduled, null, now, Rules).Confidence);
        Assert.Equal(JourneyConfidence.Confirmed, LiveStatus.Evaluate(JourneyLifecycle.Completed, JourneyConfidence.Confirmed, now.AddDays(-1), now, Rules).Confidence);
        Assert.Equal(JourneyConfidence.Unknown, LiveStatus.Evaluate(JourneyLifecycle.Running, JourneyConfidence.Live, null, now, Rules).Confidence);

        // An interrupted train has fresh data but no honest ETA (REQ-LIVE-013).
        var interrupted = LiveStatus.Evaluate(JourneyLifecycle.Interrupted, JourneyConfidence.Live, now, now, Rules);
        Assert.Equal(JourneyConfidence.Live, interrupted.Confidence);
        Assert.False(interrupted.ShowLiveTimes);
    }

    [Fact]
    public void A_position_refreshes_the_last_update_of_a_running_journey_only()
    {
        var running = Ready();
        Apply(running, StopEventType.Departed, 1, Sample.Departure);
        var later = Now.AddSeconds(45);

        Assert.True(running.RecordPosition(later).IsSuccess);
        Assert.Equal(later, running.LastAcceptedUpdateAt);

        var ready = Ready();
        ready.RecordPosition(later);
        Assert.Null(ready.LastAcceptedUpdateAt);
    }
}
