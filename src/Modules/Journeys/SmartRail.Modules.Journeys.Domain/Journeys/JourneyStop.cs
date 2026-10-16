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
}

public sealed record JourneyStopDraft(
    int Sequence,
    string StationCode,
    JourneyStopKind Kind,
    DateTimeOffset? ScheduledArrivalUtc,
    DateTimeOffset? ScheduledDepartureUtc,
    int DwellMinutes,
    string? PlatformCode);
