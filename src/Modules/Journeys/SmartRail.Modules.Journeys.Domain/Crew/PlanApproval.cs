namespace SmartRail.Modules.Journeys.Domain.Crew;

// The scheduler's approval of one day's driver and unit plan. Until a day is approved, drivers do not see their duties for it.
public sealed class PlanApproval
{
    private PlanApproval() { }

    public PlanApproval(DateOnly serviceDate, string approvedBy, DateTimeOffset approvedAt)
    {
        ServiceDate = serviceDate;
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
    }

    public DateOnly ServiceDate { get; private set; }
    public string ApprovedBy { get; private set; } = string.Empty;
    public DateTimeOffset ApprovedAt { get; private set; }
}
