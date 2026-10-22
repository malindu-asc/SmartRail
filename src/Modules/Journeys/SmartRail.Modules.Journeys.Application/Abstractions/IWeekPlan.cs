namespace SmartRail.Modules.Journeys.Application.Abstractions;

// Fills in the driver and unit plan for the given days from the predefined pattern. It never overwrites an assignment that exists.
public interface IWeekPlanner
{
    Task<int> PlanAsync(IReadOnlyList<DateOnly> dates, CancellationToken cancellationToken = default);
}

// Which days the scheduler has approved. Drivers only see duties on approved days.
public interface IPlanApprovals
{
    Task<IReadOnlySet<DateOnly>> ApprovedAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task ApproveAsync(IReadOnlyList<DateOnly> dates, string approvedBy, CancellationToken cancellationToken = default);
}
