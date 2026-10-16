using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Domain.Crew;

// State model 6: Rostered, SignedOn, OnJourney, SignedOff, Orphaned. Sign-on and the later states arrive with the driver app.
public enum DutyState
{
    Rostered = 0,
    SignedOn = 1,
    OnJourney = 2,
    SignedOff = 3,
    Orphaned = 4
}

public enum DutyReviewState
{
    None = 0,
    NeedsReview = 1,
    Reviewed = 2
}

// A driver's shift. A driver's rostered windows cannot overlap (the database refuses it).
public sealed class CrewDuty : Entity<Guid>
{
    private CrewDuty() { }

    private CrewDuty(Guid id) : base(id) { }

    public string DriverSubject { get; private set; } = string.Empty;
    public DateOnly ServiceDate { get; private set; }
    public DateTimeOffset RosteredFrom { get; private set; }
    public DateTimeOffset RosteredTo { get; private set; }
    public DutyState State { get; private set; } = DutyState.Rostered;
    public DateTimeOffset? SignedOnAt { get; private set; }
    public DateTimeOffset? SignedOffAt { get; private set; }
    public Guid? HandoverFromDutyId { get; private set; }
    public DutyReviewState ReviewState { get; private set; } = DutyReviewState.None;

    public static Result<CrewDuty> Create(string driverSubject, DateOnly serviceDate, DateTimeOffset rosteredFrom, DateTimeOffset rosteredTo, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(driverSubject) || driverSubject.Length > 64)
            return JourneysErrors.InvalidDriver;

        if (rosteredTo <= rosteredFrom)
            return JourneysErrors.InvalidPeriod;

        return new CrewDuty(id ?? Guid.NewGuid())
        {
            DriverSubject = driverSubject.Trim(),
            ServiceDate = serviceDate,
            RosteredFrom = rosteredFrom,
            RosteredTo = rosteredTo
        };
    }

    public bool Covers(DateTimeOffset from, DateTimeOffset to) => RosteredFrom <= from && to <= RosteredTo;
}

// Time-ranged. Two assignments on one journey cannot overlap, so a handover closes the outgoing duty first.
public sealed class DriverAssignment : Entity<Guid>
{
    private DriverAssignment() { }

    internal DriverAssignment(Guid journeyId, Guid dutyId, DateTimeOffset validFrom, DateTimeOffset? validTo, string assignedBy)
        : base(Guid.NewGuid())
    {
        JourneyId = journeyId;
        DutyId = dutyId;
        ValidFrom = validFrom;
        ValidTo = validTo;
        AssignedBy = assignedBy;
    }

    public Guid JourneyId { get; private set; }
    public Guid DutyId { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }
    public string AssignedBy { get; private set; } = string.Empty;

    public bool Covers(DateTimeOffset at) => ValidFrom <= at && (ValidTo is null || ValidTo > at);

    public bool Overlaps(DateTimeOffset from, DateTimeOffset? to) =>
        ValidFrom < (to ?? DateTimeOffset.MaxValue) && from < (ValidTo ?? DateTimeOffset.MaxValue);

    internal Result Close(DateTimeOffset at)
    {
        if (at <= ValidFrom)
            return JourneysErrors.InvalidPeriod;

        ValidTo = at;
        return Result.Success();
    }
}
