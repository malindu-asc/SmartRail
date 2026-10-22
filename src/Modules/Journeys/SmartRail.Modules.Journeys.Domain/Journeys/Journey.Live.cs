using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.Modules.Journeys.Domain.Live;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;
using SubState = SmartRail.Modules.Journeys.Domain.Journeys.RunningSubState;

namespace SmartRail.Modules.Journeys.Domain.Journeys;

// Live data from the train detection feed (SPEC-005): stop events, delay, punctuality and ETAs.
// Scheduled times are never written here (REQ-LIVE-017).
public sealed partial class Journey
{
    public bool AcceptsLiveEvents => Lifecycle is JourneyLifecycle.Ready or JourneyLifecycle.Running or JourneyLifecycle.Interrupted;

    // Applies a confirmed stop event. A better-ranked source that replaces an earlier value goes through the same method:
    // it overwrites the actual time and the whole live picture is recalculated from the actual times.
    public Result<StopEventOutcome> ApplyStopEvent(
        StopEventType type,
        int stopSequence,
        DateTimeOffset occurredAt,
        DateTimeOffset now,
        LiveRules rules)
    {
        if (Lifecycle == JourneyLifecycle.Planned)
            return JourneysErrors.JourneyNotReady;

        if (!AcceptsLiveEvents)
            return JourneysErrors.JourneyEnded(Lifecycle.ToString());

        var stop = _stops.FirstOrDefault(s => s.Sequence == stopSequence);
        if (stop is null)
            return JourneysErrors.StopNotFound(stopSequence);

        var firstSequence = _stops.Min(s => s.Sequence);
        var lastSequence = _stops.Max(s => s.Sequence);
        var delayBefore = DelayMinutes;
        var departedNow = false;

        // The train is clearly running once it leaves the origin, or is confirmed anywhere after it.
        if (Lifecycle == JourneyLifecycle.Ready
            && (stopSequence > firstSequence || type is StopEventType.Departed or StopEventType.Passed))
        {
            var moved = TransitionTo(JourneyLifecycle.Running);
            if (moved.IsFailure)
                return moved.Error;

            departedNow = true;
        }

        switch (type)
        {
            case StopEventType.Arrived:
                stop.RecordArrival(occurredAt);
                break;
            case StopEventType.Departed:
                stop.RecordDeparture(occurredAt);
                break;
            default:
                stop.RecordPassed();
                break;
        }

        // A lost event never leaves a stop Pending forever (state-models.md section 2).
        var inferred = new List<int>();
        foreach (var earlier in _stops.Where(s => s.Sequence < stopSequence && s.State == JourneyStopState.Pending))
        {
            earlier.RecordPassed();
            inferred.Add(earlier.Sequence);
        }

        if (CurrentStopSequence is null || stopSequence >= CurrentStopSequence)
        {
            CurrentStopSequence = stopSequence;
            if (Lifecycle == JourneyLifecycle.Running)
                RunningSubState = type == StopEventType.Arrived ? SubState.AtStop : SubState.InSection;
        }

        LastAcceptedUpdateAt = now;

        var completedNow = false;
        if (type == StopEventType.Arrived && stopSequence == lastSequence && Lifecycle == JourneyLifecycle.Running)
        {
            var finished = TransitionTo(JourneyLifecycle.Completed);
            if (finished.IsFailure)
                return finished.Error;

            EndedAt = now;
            completedNow = true;
        }

        Recalculate(rules);
        Touch();

        if (departedNow)
            RaiseDomainEvent(new JourneyDepartedDomainEvent(Id, occurredAt));

        var delayChanged = delayBefore != DelayMinutes;
        if (delayChanged)
            RaiseDomainEvent(new JourneyDelayChangedDomainEvent(Id, DelayMinutes, PunctualityBand));

        if (completedNow)
            RaiseDomainEvent(new JourneyCompletedDomainEvent(Id, occurredAt));

        return new StopEventOutcome(inferred, departedNow, completedNow, delayChanged);
    }

    // Moves a stop to another platform before or while the train runs. Scheduled times are untouched.
    public Result ChangePlatform(int stopSequence, string platformCode, string changedBy, DateTimeOffset now)
    {
        var stop = _stops.FirstOrDefault(s => s.Sequence == stopSequence);
        if (stop is null)
            return JourneysErrors.StopNotFound(stopSequence);

        var previous = stop.PlatformCode;
        stop.ChangePlatform(platformCode.Trim(), changedBy, now);
        Touch();
        RaiseDomainEvent(new JourneyPlatformChangedDomainEvent(Id, stopSequence, stop.StationCode, stop.PlatformCode!, previous));
        return Result.Success();
    }

    // An accepted position proves the feed is alive for this journey. It moves the running sub-state, nothing else.
    public Result RecordPosition(DateTimeOffset now)
    {
        if (Lifecycle != JourneyLifecycle.Running)
            return Result.Success();

        LastAcceptedUpdateAt = now;
        RunningSubState = SubState.InSection;
        Touch();
        return Result.Success();
    }

    // The station codes of the journey in order, passing points included: the sections a vehicle can be in.
    public IReadOnlyList<string> StationCodesInOrder() => _stops.OrderBy(s => s.Sequence).Select(s => s.StationCode).ToList();

    // Delay, band, per-stop estimates and journey confidence, all derived from the actual times so the result does not
    // depend on the order the messages arrived in.
    private void Recalculate(LiveRules rules)
    {
        var ordered = _stops.OrderBy(s => s.Sequence).ToList();

        // Delay: the newest stop that has an actual time to compare with its scheduled time.
        int? delay = null;
        foreach (var stop in Enumerable.Reverse(ordered))
        {
            var difference = stop.DelayAgainstSchedule();
            if (difference is not null)
            {
                delay = (int)Math.Round(difference.Value.TotalMinutes, MidpointRounding.AwayFromZero);
                break;
            }
        }

        var showsDelay = Lifecycle is JourneyLifecycle.Running or JourneyLifecycle.Completed && delay is not null;
        DelayMinutes = showsDelay ? delay : null;
        PunctualityBand = showsDelay ? LiveRules.BandFor(delay!.Value) : null;

        Confidence = Lifecycle switch
        {
            JourneyLifecycle.Completed => JourneyConfidence.Confirmed,
            JourneyLifecycle.Running or JourneyLifecycle.Interrupted => JourneyConfidence.Live,
            _ => JourneyConfidence.Scheduled
        };

        foreach (var stop in ordered)
            stop.ClearEstimate();

        // ETAs exist only while the train is running; an interrupted train has no honest ETA (REQ-LIVE-013).
        if (Lifecycle != JourneyLifecycle.Running)
            return;

        var anchor = ordered.LastOrDefault(s => s.State is JourneyStopState.Arrived or JourneyStopState.Departed);
        if (anchor is null)
            return;

        TimeSpan carried;
        if (anchor.State == JourneyStopState.Departed)
        {
            carried = anchor.DepartureDelay() ?? TimeSpan.Zero;
        }
        else
        {
            // At the platform: it cannot leave before the minimum dwell has passed, and never before scheduled.
            var departure = Latest(anchor.ScheduledDepartureUtc, anchor.ActualArrivalUtc?.AddMinutes(MinDwell(anchor, rules)));
            anchor.SetEstimate(null, departure);
            carried = departure is not null && anchor.ScheduledDepartureUtc is not null
                ? departure.Value - anchor.ScheduledDepartureUtc.Value
                : TimeSpan.Zero;
        }

        foreach (var stop in ordered.Where(s => s.Sequence > anchor.Sequence && s.State == JourneyStopState.Pending))
        {
            DateTimeOffset? arrival = stop.ScheduledArrivalUtc?.Add(carried);
            var departure = stop.ScheduledDepartureUtc is null
                ? (DateTimeOffset?)null
                : Latest(stop.ScheduledDepartureUtc, arrival?.AddMinutes(MinDwell(stop, rules)));

            stop.SetEstimate(arrival, departure);

            // What is left of the delay after this stop's dwell has absorbed what it can.
            if (departure is not null && stop.ScheduledDepartureUtc is not null)
                carried = departure.Value - stop.ScheduledDepartureUtc.Value;
        }
    }

    private static int MinDwell(JourneyStop stop, LiveRules rules) =>
        stop.Kind == JourneyStopKind.Passing ? 0 : rules.MinDwellMinutes;

    // A departure is never shown earlier than scheduled (REQ-LIVE-012).
    private static DateTimeOffset? Latest(DateTimeOffset? scheduled, DateTimeOffset? candidate) =>
        scheduled is null ? candidate : candidate is null || candidate < scheduled ? scheduled : candidate;
}

public sealed record StopEventOutcome(IReadOnlyList<int> InferredStops, bool Departed, bool Completed, bool DelayChanged);

public sealed record JourneyDepartedDomainEvent(Guid JourneyId, DateTimeOffset At) : DomainEvent;

public sealed record JourneyCompletedDomainEvent(Guid JourneyId, DateTimeOffset At) : DomainEvent;

public sealed record JourneyDelayChangedDomainEvent(Guid JourneyId, int? DelayMinutes, PunctualityBand? Band) : DomainEvent;

public sealed record JourneyPlatformChangedDomainEvent(Guid JourneyId, int StopSequence, string StationCode, string Platform, string? PreviousPlatform) : DomainEvent;

public static class DelayReasons
{
    public static readonly IReadOnlyList<string> All = ["signal_fault", "door_fault", "crew", "passenger_incident", "track", "other"];
}

public sealed partial class Journey
{
    // The driver says why the train is late. The controller sees it on the board; the last one wins.
    public Result RecordDelayReason(string code, DateTimeOffset now)
    {
        if (!DelayReasons.All.Contains(code))
            return JourneysErrors.DelayReasonUnknown;
        if (JourneyLifecycleRules.IsFinal(Lifecycle) || Lifecycle == JourneyLifecycle.Cancelled)
            return JourneysErrors.JourneyEnded(Lifecycle.ToString());

        DelayReasonCode = code;
        DelayReasonAt = now;
        Touch();
        return Result.Success();
    }
}
