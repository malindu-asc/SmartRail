namespace SmartRail.Modules.Journeys.Domain.Journeys;

// One stop of a journey. Key: (JourneyId, Sequence).
// Scheduled times are set when the journey is created and never change (REQ-LIVE-002, REQ-LIVE-017).
public sealed class JourneyStop
{
    private JourneyStop() { }

    internal JourneyStop(Guid journeyId, JourneyStopDraft draft)
    {
        JourneyId = journeyId;
        Sequence = draft.Sequence;
        StationCode = draft.StationCode;
        Kind = draft.Kind;
        State = JourneyStopState.Pending;
        ScheduledArrivalUtc = draft.ScheduledArrivalUtc;
        ScheduledDepartureUtc = draft.ScheduledDepartureUtc;
        EstimateSource = EstimateSource.Scheduled;
        DwellMinutes = draft.DwellMinutes;
        PlatformCode = draft.PlatformCode;
        PlatformOriginalCode = draft.PlatformCode;
        PlatformState = draft.PlatformCode is null ? PlatformState.TBA : PlatformState.Assigned;
    }

    public Guid JourneyId { get; private set; }
    public int Sequence { get; private set; }
    public string StationCode { get; private set; } = string.Empty;
    public JourneyStopKind Kind { get; private set; }
    public JourneyStopState State { get; private set; }
    public DateTimeOffset? ScheduledArrivalUtc { get; private set; }
    public DateTimeOffset? ScheduledDepartureUtc { get; private set; }
    public DateTimeOffset? EstimatedArrivalUtc { get; private set; }
    public DateTimeOffset? EstimatedDepartureUtc { get; private set; }
    public DateTimeOffset? ActualArrivalUtc { get; private set; }
    public DateTimeOffset? ActualDepartureUtc { get; private set; }
    public EstimateSource EstimateSource { get; private set; }
    public int DwellMinutes { get; private set; }
    public string? PlatformCode { get; private set; }
    public PlatformState PlatformState { get; private set; }
    public string? PlatformOriginalCode { get; private set; }
    public DateTimeOffset? PlatformChangedAt { get; private set; }
    public string? PlatformChangedBy { get; private set; }

    // A new platform for this stop. The first platform stays on record so clients can say "changed from 2".
    internal void ChangePlatform(string code, string changedBy, DateTimeOffset at)
    {
        PlatformCode = code;
        PlatformState = PlatformState.Changed;
        PlatformChangedAt = at;
        PlatformChangedBy = changedBy;
    }

    // ---- live data (SPEC-005). Scheduled times are never touched. ----

    internal void RecordArrival(DateTimeOffset at)
    {
        ActualArrivalUtc = at;
        if (State == JourneyStopState.Pending)
            State = JourneyStopState.Arrived;
    }

    internal void RecordDeparture(DateTimeOffset at)
    {
        ActualDepartureUtc = at;
        State = JourneyStopState.Departed;
    }

    // The feed saw the train go through, or a later stop was confirmed: the stop is behind the train, with no actual time.
    internal void RecordPassed()
    {
        if (State is JourneyStopState.Pending or JourneyStopState.Arrived)
            State = JourneyStopState.Departed;
    }

    // Actual minus scheduled for the newest comparable pair at this stop; null when there is nothing to compare.
    internal TimeSpan? DelayAgainstSchedule() =>
        DepartureDelay() ?? (ActualArrivalUtc is { } actual && ScheduledArrivalUtc is { } planned ? actual - planned : null);

    internal TimeSpan? DepartureDelay() =>
        ActualDepartureUtc is { } actual && ScheduledDepartureUtc is { } planned ? actual - planned : null;

    internal void SetEstimate(DateTimeOffset? arrival, DateTimeOffset? departure)
    {
        EstimatedArrivalUtc = arrival;
        EstimatedDepartureUtc = departure;
        EstimateSource = EstimateSource.Live;
    }

    internal void ClearEstimate()
    {
        EstimatedArrivalUtc = null;
        EstimatedDepartureUtc = null;
        EstimateSource = State is JourneyStopState.Arrived or JourneyStopState.Departed ? EstimateSource.Confirmed : EstimateSource.Scheduled;
    }
}

public sealed record JourneyStopDraft(
    int Sequence,
    string StationCode,
    JourneyStopKind Kind,
    DateTimeOffset? ScheduledArrivalUtc,
    DateTimeOffset? ScheduledDepartureUtc,
    int DwellMinutes,
    string? PlatformCode);
