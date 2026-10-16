namespace SmartRail.Modules.Network.Domain.Routes;

// Key: (RouteId, Sequence). A stop's default platform must belong to its own station; the database enforces it.
public sealed class RouteStop
{
    private RouteStop() { }

    internal RouteStop(Guid routeId, RouteStopDraft draft)
    {
        RouteId = routeId;
        Sequence = draft.Sequence;
        StationId = draft.StationId;
        DefaultPlatformId = draft.DefaultPlatformId;
        MinutesFromPrevious = draft.MinutesFromPrevious;
        DwellMinutes = draft.DwellMinutes;
        Kind = draft.Kind;
    }

    public Guid RouteId { get; private set; }
    public int Sequence { get; private set; }
    public Guid StationId { get; private set; }
    public Guid? DefaultPlatformId { get; private set; }
    public int MinutesFromPrevious { get; private set; }
    public int DwellMinutes { get; private set; }
    public StopKind Kind { get; private set; }
}

public sealed record RouteStopDraft(
    int Sequence,
    Guid StationId,
    Guid? DefaultPlatformId,
    int MinutesFromPrevious,
    int DwellMinutes,
    StopKind Kind);
