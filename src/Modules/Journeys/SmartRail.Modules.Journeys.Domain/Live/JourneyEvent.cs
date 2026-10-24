namespace SmartRail.Modules.Journeys.Domain.Live;

public enum StopEventType
{
    Arrived = 0,
    Departed = 1,
    Passed = 2
}

public enum EventSourceKind
{
    Feed = 0,
    Driver = 1,
    Controller = 2,
    System = 3
}

// What happened to a message (REQ-LIVE-005, REQ-LIVE-006). Rejected and superseded values are kept, never discarded.
public enum EventDisposition
{
    Applied = 0,
    Superseded = 1,
    Rejected = 2,
    HeldForCorrection = 3,
    Duplicate = 4,
    Orphaned = 5,
    OutsideDutyWindow = 6
}

// One row of a journey's event history. Append-only: the only later change is that a better-ranked source supersedes it.
public sealed class JourneyEvent
{
    private JourneyEvent() { }

    public JourneyEvent(
        Guid journeyId,
        StopEventType type,
        int stopSequence,
        EventSourceKind sourceKind,
        Guid sourceId,
        int sourceRank,
        DateTimeOffset occurredAtSource,
        DateTimeOffset receivedAt,
        long deviceSequence,
        EventDisposition disposition,
        string? dispositionReason,
        string payload)
    {
        JourneyId = journeyId;
        EventType = type;
        StopSequence = stopSequence;
        SourceKind = sourceKind;
        SourceId = sourceId;
        SourceRank = sourceRank;
        OccurredAtSource = occurredAtSource;
        ReceivedAt = receivedAt;
        DeviceSequence = deviceSequence;
        Disposition = disposition;
        DispositionReason = dispositionReason;
        Payload = payload;
    }

    public long Id { get; private set; }
    public Guid JourneyId { get; private set; }
    public StopEventType EventType { get; private set; }
    public int StopSequence { get; private set; }
    public EventSourceKind SourceKind { get; private set; }
    public Guid SourceId { get; private set; }
    public int SourceRank { get; private set; }

    // The feed or device time. Used for delay, never for ordering (the server clock and the sequence order messages).
    public DateTimeOffset OccurredAtSource { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public long DeviceSequence { get; private set; }
    public string Payload { get; private set; } = "{}";
    public EventDisposition Disposition { get; private set; }
    public string? DispositionReason { get; private set; }
    public long? SupersededByEventId { get; private set; }
    public JourneyEvent? SupersededBy { get; private set; }

    public void SupersedeBy(JourneyEvent winner)
    {
        Disposition = EventDisposition.Superseded;
        DispositionReason = "better_ranked_source";
        SupersededBy = winner;
    }
}
