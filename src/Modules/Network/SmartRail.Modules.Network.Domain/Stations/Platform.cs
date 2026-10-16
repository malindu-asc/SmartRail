using SmartRail.SharedKernel.Primitives;

namespace SmartRail.Modules.Network.Domain.Stations;

public sealed class Platform : Entity<Guid>
{
    private Platform() { }

    internal Platform(Guid id, Guid stationId, string code, bool isAvailable) : base(id)
    {
        StationId = stationId;
        Code = code;
        IsAvailable = isAvailable;
    }

    public Guid StationId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public bool IsAvailable { get; private set; }
}
