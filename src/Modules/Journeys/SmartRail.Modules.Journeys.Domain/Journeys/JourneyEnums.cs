namespace SmartRail.Modules.Journeys.Domain.Journeys;

// Lifecycle, punctuality and confidence are three independent dimensions (state model 1a, 1b, 1c).
public enum JourneyLifecycle
{
    Planned = 0,
    Ready = 1,
    Running = 2,
    Interrupted = 3,
    Completed = 4,
    TerminatedShort = 5,
    Cancelled = 6,
    TerminatedEmergency = 7
}

public enum RunningSubState
{
    AtStop = 0,
    InSection = 1
}

public enum PunctualityBand
{
    Early = 0,
    OnTime = 1,
    MinorDelay = 2,
    MajorDelay = 3
}

public enum JourneyConfidence
{
    Confirmed = 0,
    Live = 1,
    Scheduled = 2,
    Unknown = 3
}

public enum JourneyCreationOrigin
{
    Generated = 0,
    Manual = 1
}

public enum JourneyStopKind
{
    Stop = 0,
    PickUpOnly = 1,
    SetDownOnly = 2,
    Passing = 3
}

public enum JourneyStopState
{
    Pending = 0,
    Arrived = 1,
    Departed = 2,
    Skipped = 3,
    Cancelled = 4
}

public enum EstimateSource
{
    Confirmed = 0,
    Live = 1,
    Scheduled = 2
}

public enum PlatformState
{
    TBA = 0,
    Assigned = 1,
    Changed = 2
}
