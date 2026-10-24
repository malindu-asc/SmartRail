namespace SmartRail.Modules.Network.Domain.Timetables;

// Stored as text. Moves forward only: Draft, InReview, Published, Superseded (REQ-OPS-003).
public enum TimetableVersionStatus
{
    Draft = 0,
    InReview = 1,
    Published = 2,
    Superseded = 3
}

// Direction of a service along its route. It is internal: passengers see "to <destination>".
public enum ServiceDirection
{
    Forward = 0,
    Reverse = 1
}

// Only Regional services can be bought (REQ-TKT-008).
public enum ServiceCategory
{
    Regional = 0,
    Intercity = 1,
    HighSpeed = 2
}

public enum CalendarExceptionKind
{
    Removed = 0,
    Added = 1
}
