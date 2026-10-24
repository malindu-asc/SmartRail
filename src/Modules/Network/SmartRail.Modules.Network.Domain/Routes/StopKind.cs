namespace SmartRail.Modules.Network.Domain.Routes;

// Stored as text. Passing points exist in the working view only (identity model).
public enum StopKind
{
    Stop = 0,
    PickUpOnly = 1,
    SetDownOnly = 2,
    Passing = 3
}
