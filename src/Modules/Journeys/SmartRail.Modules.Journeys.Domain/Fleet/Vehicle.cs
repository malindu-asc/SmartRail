using System.Text.RegularExpressions;
using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Domain.Fleet;

public enum VehicleKind
{
    Trainset = 0,
    Locomotive = 1
}

// A consist (a trainset, or a locomotive with coaches). Individual coaches are out of scope (ASM-015).
public sealed partial class Vehicle : Entity<Guid>
{
    private Vehicle() { }

    private Vehicle(Guid id) : base(id) { }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public VehicleKind Kind { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Result<Vehicle> Create(string code, string name, VehicleKind kind, Guid? id = null)
    {
        if (!VehicleCodePattern().IsMatch(code))
            return JourneysErrors.InvalidVehicleCode;

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            return JourneysErrors.InvalidName;

        return new Vehicle(id ?? Guid.NewGuid()) { Code = code, Name = name.Trim(), Kind = kind };
    }

    [GeneratedRegex("^[A-Z0-9-]{2,30}$")]
    private static partial Regex VehicleCodePattern();
}

// Time-ranged: a vehicle is on a journey from one instant to another. A null end means "until further notice".
public sealed class VehicleAssignment : Entity<Guid>
{
    private VehicleAssignment() { }

    internal VehicleAssignment(Guid journeyId, Guid vehicleId, DateTimeOffset validFrom, DateTimeOffset? validTo, string assignedBy, string? reason)
        : base(Guid.NewGuid())
    {
        JourneyId = journeyId;
        VehicleId = vehicleId;
        ValidFrom = validFrom;
        ValidTo = validTo;
        AssignedBy = assignedBy;
        Reason = reason;
    }

    public Guid JourneyId { get; private set; }
    public Guid VehicleId { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }
    public string AssignedBy { get; private set; } = string.Empty;
    public string? Reason { get; private set; }

    public bool Covers(DateTimeOffset at) => ValidFrom <= at && (ValidTo is null || ValidTo > at);

    public bool Overlaps(DateTimeOffset from, DateTimeOffset? to) =>
        ValidFrom < (to ?? DateTimeOffset.MaxValue) && from < (ValidTo ?? DateTimeOffset.MaxValue);

    internal Result Close(DateTimeOffset at) =>
        at <= ValidFrom ? JourneysErrors.InvalidPeriod : Apply(at);

    private Result Apply(DateTimeOffset at)
    {
        ValidTo = at;
        return Result.Success();
    }
}
