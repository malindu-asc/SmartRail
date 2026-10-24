namespace SmartRail.Modules.Journeys.Domain.Journeys;

// The lifecycle transition table (state-models.md 1a). Anything not listed is illegal.
public static class JourneyLifecycleRules
{
    private static readonly IReadOnlyDictionary<JourneyLifecycle, JourneyLifecycle[]> Allowed =
        new Dictionary<JourneyLifecycle, JourneyLifecycle[]>
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

    public static bool IsAllowed(JourneyLifecycle from, JourneyLifecycle to) => Allowed[from].Contains(to);

    public static IReadOnlyList<JourneyLifecycle> NextFrom(JourneyLifecycle from) => Allowed[from];

    // Completed, TerminatedShort and TerminatedEmergency are final; the journey can no longer change.
    public static bool IsFinal(JourneyLifecycle state) => Allowed[state].Length == 0;

    // Passengers see Planned for both Planned and Ready (state-models.md).
    public static string PublicStatus(JourneyLifecycle state) =>
        state is JourneyLifecycle.Planned or JourneyLifecycle.Ready ? nameof(JourneyLifecycle.Planned) : state.ToString();
}
