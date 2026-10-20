using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Domain.Fleet;

// A unit that cannot be used for a period: a fault, maintenance, an accident (REQ-OPS-012). A null end means "until further notice".
public sealed class VehicleUnavailability : Entity<Guid>
{
    private VehicleUnavailability() { }

    private VehicleUnavailability(Guid id) : base(id) { }

    public Guid VehicleId { get; private set; }
    public DateTimeOffset From { get; private set; }
    public DateTimeOffset? To { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }

    public static Result<VehicleUnavailability> Create(Guid vehicleId, DateTimeOffset from, DateTimeOffset? to, string reason, string createdBy, DateTimeOffset now, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return JourneysErrors.ReasonRequired;
        if (to is not null && to <= from)
            return JourneysErrors.InvalidPeriod;

        return new VehicleUnavailability(id ?? Guid.NewGuid())
        {
            VehicleId = vehicleId,
            From = from,
            To = to,
            Reason = reason.Trim()[..Math.Min(reason.Trim().Length, 200)],
            CreatedBy = createdBy,
            CreatedAt = now
        };
    }

    public bool CoversAt(DateTimeOffset at) => EndedAt is null && From <= at && (To is null || To > at);

    public bool Overlaps(DateTimeOffset from, DateTimeOffset to) =>
        EndedAt is null && From < to && from < (To ?? DateTimeOffset.MaxValue);

    // The unit is back in service now.
    public Result End(DateTimeOffset now)
    {
        if (EndedAt is not null)
            return JourneysErrors.InvalidPeriod;

        EndedAt = now;
        return Result.Success();
    }
}
