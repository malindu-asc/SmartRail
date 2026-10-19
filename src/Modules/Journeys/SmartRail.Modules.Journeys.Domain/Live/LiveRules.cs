using SmartRail.Modules.Journeys.Domain.Journeys;

namespace SmartRail.Modules.Journeys.Domain.Live;

// The numbers behind live data (ASM-019, ASM-020, ASM-021, ASM-032). Bound from configuration "Journeys:Live".
public sealed record LiveRules(
    int StaleAfterSeconds = 60,
    int UnknownAfterSeconds = 300,
    int MinDwellMinutes = 1,
    double MaxSpeedKmh = 160,
    int FutureToleranceSeconds = 5,
    double MaxProgressRegression = 0.02)
{
    public static LiveRules Default { get; } = new();

    // Punctuality bands: state-models.md section 1b.
    public static PunctualityBand BandFor(int delayMinutes) => delayMinutes switch
    {
        <= -2 => PunctualityBand.Early,
        < 2 => PunctualityBand.OnTime,
        < 10 => PunctualityBand.MinorDelay,
        _ => PunctualityBand.MajorDelay
    };
}

// What a client may be told about the freshness of a journey right now. Derived from the clock when a response is built,
// so Stale and Unknown need no background job (decision D4).
public sealed record LiveStatus(JourneyConfidence Confidence, bool IsStale, int? AgeSeconds, bool ShowLiveTimes)
{
    public static LiveStatus Evaluate(
        JourneyLifecycle lifecycle,
        JourneyConfidence stored,
        DateTimeOffset? lastAcceptedUpdateAt,
        DateTimeOffset now,
        LiveRules rules)
    {
        if (lifecycle is not (JourneyLifecycle.Running or JourneyLifecycle.Interrupted))
            return new LiveStatus(stored, false, null, ShowLiveTimes: lifecycle == JourneyLifecycle.Completed);

        // A running journey that has never had an accepted update has no live basis.
        if (lastAcceptedUpdateAt is null)
            return new LiveStatus(JourneyConfidence.Unknown, false, null, false);

        var age = (int)Math.Max(0, (now - lastAcceptedUpdateAt.Value).TotalSeconds);

        if (age >= rules.UnknownAfterSeconds)
            return new LiveStatus(JourneyConfidence.Unknown, false, age, false);

        // Interrupted: the last position is frozen and the ETA is unknown, but the data itself can be fresh (REQ-LIVE-013).
        return new LiveStatus(JourneyConfidence.Live, age >= rules.StaleAfterSeconds, age, lifecycle == JourneyLifecycle.Running);
    }
}
