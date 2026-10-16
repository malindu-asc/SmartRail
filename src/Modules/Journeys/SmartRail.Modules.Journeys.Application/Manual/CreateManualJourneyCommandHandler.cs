using SmartRail.Application.Core.Abstractions;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Manual;

public sealed class CreateManualJourneyCommandHandler(
    INetworkRoutes routes,
    INetworkTimeZone timeZone,
    IJourneyRepository journeys,
    IJourneysUnitOfWork unitOfWork,
    IJourneysAuditLog audit,
    ICurrentUser currentUser)
    : ICommandHandler<CreateManualJourneyCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateManualJourneyCommand command, CancellationToken cancellationToken)
    {
        var routeCode = command.RouteCode.Trim().ToUpperInvariant();
        var route = await routes.GetStationCodesAsync(routeCode, cancellationToken);
        if (route is null)
            return JourneysErrors.RouteNotFound(routeCode);

        var stationCodes = command.Stops.Select(s => s.StationCode.Trim().ToUpperInvariant()).ToList();
        if (!FollowsRoute(route, stationCodes))
            return JourneysErrors.StopsNotOnRoute;

        var drafts = new List<JourneyStopDraft>();
        for (var i = 0; i < command.Stops.Count; i++)
        {
            var stop = command.Stops[i];
            var arrival = ToUtc(stop.Arrival, command.ServiceDate);
            var departure = ToUtc(stop.Departure, command.ServiceDate);
            var dwell = arrival is { } a && departure is { } d ? (int)(d - a).TotalMinutes : 0;

            drafts.Add(new JourneyStopDraft(i + 1, stationCodes[i], JourneyStopKind.Stop, arrival, departure, dwell,
                string.IsNullOrWhiteSpace(stop.Platform) ? null : stop.Platform.Trim()));
        }

        var journey = Journey.Create(null, null, command.ServiceDate, command.TrainNumber.Trim(), routeCode,
            JourneyCreationOrigin.Manual, command.Reason, drafts);
        if (journey.IsFailure)
            return journey.Error;

        journeys.Add(journey.Value);

        // The audit row is saved in the same transaction as the journey (REQ-OPS-011).
        audit.Record(new AuditRecord(
            currentUser.UserId ?? "unknown",
            currentUser.Roles.FirstOrDefault(),
            "journey.manual.create",
            "journey",
            journey.Value.Id.ToString(),
            command.Reason,
            After: new
            {
                journey.Value.TrainNumber,
                journey.Value.RouteCode,
                ServiceDate = command.ServiceDate.ToString("yyyy-MM-dd"),
                Stops = stationCodes
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return journey.Value.Id;
    }

    private DateTimeOffset? ToUtc(string? time, DateOnly date) =>
        ManualTime.TryParse(time, out var minutes) && !string.IsNullOrWhiteSpace(time)
            ? timeZone.ToUtc(date, minutes)
            : null;

    // The stops may skip stations but must keep the route's order, forward or reverse.
    private static bool FollowsRoute(IReadOnlyList<string> route, IReadOnlyList<string> stops)
    {
        foreach (var ordered in new[] { route.ToList(), route.Reverse().ToList() })
        {
            var position = -1;
            var ok = true;
            foreach (var stop in stops)
            {
                var found = ordered.IndexOf(stop);
                if (found <= position)
                {
                    ok = false;
                    break;
                }

                position = found;
            }

            if (ok)
                return true;
        }

        return false;
    }
}
