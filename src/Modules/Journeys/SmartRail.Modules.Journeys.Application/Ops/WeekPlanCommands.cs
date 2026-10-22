using SmartRail.Application.Core.Abstractions;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Ops;

// ---- The scheduler's week plan: generate it from the predefined pattern, edit it, approve it ----

public sealed record PlanResult(int Assigned);

public sealed record GeneratePlanCommand(DateOnly? From) : ICommand<PlanResult>;

// Fills in the drivers and units for seven days from the predefined pattern. Assignments that already exist are left as they are.
public sealed class GeneratePlanCommandHandler(IWeekPlanner planner, INetworkTimeZone timeZone) : ICommandHandler<GeneratePlanCommand, PlanResult>
{
    public async Task<Result<PlanResult>> Handle(GeneratePlanCommand command, CancellationToken cancellationToken)
    {
        var from = command.From ?? timeZone.Today();
        var assigned = await planner.PlanAsync(Enumerable.Range(0, 7).Select(from.AddDays).ToList(), cancellationToken);
        return new PlanResult(assigned);
    }
}

public sealed record ApprovePlanResult(int Days);

public sealed record ApprovePlanCommand(DateOnly? From) : ICommand<ApprovePlanResult>;

// Approves the seven days. From then on the drivers see their duties for those days.
public sealed class ApprovePlanCommandHandler(IPlanApprovals approvals, ICurrentUser user, INetworkTimeZone timeZone)
    : ICommandHandler<ApprovePlanCommand, ApprovePlanResult>
{
    public async Task<Result<ApprovePlanResult>> Handle(ApprovePlanCommand command, CancellationToken cancellationToken)
    {
        var from = command.From ?? timeZone.Today();
        var days = Enumerable.Range(0, 7).Select(from.AddDays).ToList();
        await approvals.ApproveAsync(days, user.UserId ?? "unknown", cancellationToken);
        return new ApprovePlanResult(days.Count);
    }
}
