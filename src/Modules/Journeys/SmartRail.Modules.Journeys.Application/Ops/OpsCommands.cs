using SmartRail.Application.Core.Abstractions;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Ops;

public sealed record OpsActionResult(Guid JourneyId, string Lifecycle, int Version);

// Runs one controller action on one journey: check the version the screen was looking at (REQ-OPS-010), apply the change, write the
// audit row in the same transaction (REQ-OPS-011), save. If another editor got there first the caller gets a conflict.
public sealed class JourneyActions(
    IJourneyRepository journeys,
    IOpsJourneyReader reader,
    IJourneysUnitOfWork unitOfWork,
    IJourneysAuditLog audit,
    ICurrentUser user,
    TimeProvider clock)
{
    public async Task<Result<OpsActionResult>> RunAsync(
        Guid journeyId, int? expectedVersion, string action, string? reason,
        Func<Journey, DateTimeOffset, Result> apply, object? extraAfter, CancellationToken cancellationToken)
    {
        var journey = await journeys.GetWithStopsAsync(journeyId, cancellationToken);
        if (journey is null)
            return JourneysErrors.JourneyNotFound(journeyId);

        if (expectedVersion is { } expected && expected != journey.Version)
            return JourneysErrors.VersionConflict(journey.Version);

        var before = new { Lifecycle = journey.Lifecycle.ToString(), journey.Version, journey.DelayMinutes };
        var result = apply(journey, clock.GetUtcNow());
        if (result.IsFailure)
            return result.Error;

        audit.Record(new AuditRecord(
            user.UserId ?? "unknown", user.Roles.FirstOrDefault(), action, "journey", journey.Id.ToString(), Trim(reason),
            Before: before,
            After: new { Lifecycle = journey.Lifecycle.ToString(), journey.TrainNumber, journey.RouteCode, Detail = extraAfter }));

        if (!await unitOfWork.TrySaveAsync(cancellationToken))
        {
            var current = await reader.GetAsync(journeyId, cancellationToken);
            return JourneysErrors.VersionConflict(current?.Version ?? journey.Version);
        }

        return new OpsActionResult(journey.Id, journey.Lifecycle.ToString(), journey.Version);
    }

    public static string? Reason(IReadOnlyList<ReasonCode> catalog, string code, string? note, out Error? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(code))
        {
            error = JourneysErrors.ReasonRequired;
            return null;
        }

        var match = catalog.FirstOrDefault(r => string.Equals(r.Code, code.Trim(), StringComparison.Ordinal));
        if (match is null)
        {
            error = JourneysErrors.ReasonCodeUnknown(code);
            return null;
        }

        return string.IsNullOrWhiteSpace(note) ? match.Label : $"{match.Label}: {note.Trim()}";
    }

    private static string? Trim(string? text) => text is null ? null : text.Length <= 500 ? text : text[..500];
}

// ---- Cancel (before departure, or an interrupted train) ----

public sealed record CancelJourneyCommand(Guid JourneyId, string ReasonCode, string? Note, string? PassengerMessage, int? ExpectedVersion)
    : ICommand<OpsActionResult>;

public sealed class CancelJourneyCommandHandler(JourneyActions actions) : ICommandHandler<CancelJourneyCommand, OpsActionResult>
{
    public async Task<Result<OpsActionResult>> Handle(CancelJourneyCommand command, CancellationToken cancellationToken)
    {
        var reason = JourneyActions.Reason(ReasonCatalog.Cancel, command.ReasonCode, command.Note, out var error);
        if (error is not null)
            return error;

        return await actions.RunAsync(command.JourneyId, command.ExpectedVersion, "journey.cancel", reason,
            (j, _) => j.CancelWithNote(command.ReasonCode, command.PassengerMessage ?? command.Note), new { command.PassengerMessage }, cancellationToken);
    }
}

// ---- Terminate short ----

public sealed record TerminateShortCommand(Guid JourneyId, int StopSequence, string ReasonCode, string? Note, int? ExpectedVersion)
    : ICommand<OpsActionResult>;

public sealed class TerminateShortCommandHandler(JourneyActions actions) : ICommandHandler<TerminateShortCommand, OpsActionResult>
{
    public async Task<Result<OpsActionResult>> Handle(TerminateShortCommand command, CancellationToken cancellationToken)
    {
        var reason = JourneyActions.Reason(ReasonCatalog.TerminateShort, command.ReasonCode, command.Note, out var error);
        if (error is not null)
            return error;

        return await actions.RunAsync(command.JourneyId, command.ExpectedVersion, "journey.terminate_short", reason,
            (j, now) => j.TerminateShort(command.StopSequence, command.ReasonCode, command.Note, now), new { command.StopSequence }, cancellationToken);
    }
}

// ---- Interrupt and resume ----

public sealed record InterruptJourneyCommand(Guid JourneyId, string ReasonCode, string? Note, int? ExpectedVersion) : ICommand<OpsActionResult>;

public sealed class InterruptJourneyCommandHandler(JourneyActions actions) : ICommandHandler<InterruptJourneyCommand, OpsActionResult>
{
    public async Task<Result<OpsActionResult>> Handle(InterruptJourneyCommand command, CancellationToken cancellationToken)
    {
        var reason = JourneyActions.Reason(ReasonCatalog.Interrupt, command.ReasonCode, command.Note, out var error);
        if (error is not null)
            return error;

        return await actions.RunAsync(command.JourneyId, command.ExpectedVersion, "journey.interrupt", reason,
            (j, now) => j.Interrupt(command.ReasonCode, command.Note, now), null, cancellationToken);
    }
}

public sealed record ResumeJourneyCommand(Guid JourneyId, int? ExpectedVersion) : ICommand<OpsActionResult>;

public sealed class ResumeJourneyCommandHandler(JourneyActions actions) : ICommandHandler<ResumeJourneyCommand, OpsActionResult>
{
    public Task<Result<OpsActionResult>> Handle(ResumeJourneyCommand command, CancellationToken cancellationToken) =>
        actions.RunAsync(command.JourneyId, command.ExpectedVersion, "journey.resume", "Resumed after interruption",
            (j, now) => j.Resume(now), null, cancellationToken);
}

// ---- Reinstate a cancelled journey (before its scheduled departure) ----

public sealed record ReinstateJourneyCommand(Guid JourneyId, string Reason, int? ExpectedVersion) : ICommand<OpsActionResult>;

public sealed class ReinstateJourneyCommandHandler(JourneyActions actions) : ICommandHandler<ReinstateJourneyCommand, OpsActionResult>
{
    public Task<Result<OpsActionResult>> Handle(ReinstateJourneyCommand command, CancellationToken cancellationToken) =>
        actions.RunAsync(command.JourneyId, command.ExpectedVersion, "journey.reinstate", command.Reason,
            (j, now) => j.ReinstateWithReason(now, command.Reason), null, cancellationToken);
}

// ---- Emergency termination (declared through an incident) ----

public sealed record TerminateEmergencyCommand(Guid JourneyId, string Reason) : ICommand<OpsActionResult>;

public sealed class TerminateEmergencyCommandHandler(JourneyActions actions, ITelemetryHold hold) : ICommandHandler<TerminateEmergencyCommand, OpsActionResult>
{
    public async Task<Result<OpsActionResult>> Handle(TerminateEmergencyCommand command, CancellationToken cancellationToken)
    {
        var result = await actions.RunAsync(command.JourneyId, null, "journey.terminate_emergency", command.Reason,
            (j, now) => j.TerminateEmergency("emergency", now), null, cancellationToken);

        // The raw telemetry of this journey is kept as evidence: the retention purge skips it (REQ-OPS-008).
        if (result.IsSuccess)
            await hold.PlaceAsync(command.JourneyId, cancellationToken);

        return result;
    }
}

// ---- Change platform (REQ-OPS-004), with a check before it clashes with another train ----

public sealed record PlatformConflict(Guid JourneyId, string TrainNumber, string DestinationStationCode, DateTimeOffset From, DateTimeOffset To);

public sealed record PlatformCheck(string StationCode, string Platform, string? CurrentPlatform, bool Available, IReadOnlyList<PlatformConflict> Conflicts);

internal static class PlatformRules
{
    // Two trains on one platform need a gap: a train is on the platform from a little before it arrives until a little after it leaves.
    public static readonly TimeSpan Margin = TimeSpan.FromMinutes(2);

    public static (DateTimeOffset From, DateTimeOffset To) Window(OpsStopData stop)
    {
        var from = stop.ScheduledArrivalUtc ?? stop.ScheduledDepartureUtc ?? DateTimeOffset.MinValue;
        var to = stop.ScheduledDepartureUtc ?? stop.ScheduledArrivalUtc ?? DateTimeOffset.MinValue;
        var late = stop.EstimatedDepartureUtc ?? stop.EstimatedArrivalUtc;
        if (late is { } l && l > to)
            to = l;
        return (from - Margin, to + Margin);
    }
}

public sealed record CheckPlatformQuery(Guid JourneyId, int StopSequence, string Platform) : IQuery<PlatformCheck>;

public sealed class CheckPlatformQueryHandler(IOpsJourneyReader reader, INetworkStations stations) : IQueryHandler<CheckPlatformQuery, PlatformCheck>
{
    public async Task<Result<PlatformCheck>> Handle(CheckPlatformQuery query, CancellationToken cancellationToken)
    {
        var j = await reader.GetAsync(query.JourneyId, cancellationToken);
        if (j is null)
            return JourneysErrors.JourneyNotFound(query.JourneyId);

        var stop = j.Stops.FirstOrDefault(s => s.Sequence == query.StopSequence);
        if (stop is null)
            return JourneysErrors.StopNotFound(query.StopSequence);
        if (stop.Kind == JourneyStopKind.Passing)
            return JourneysErrors.StopHasNoPlatform;

        var platform = query.Platform.Trim();
        var valid = await stations.GetPlatformsAsync(stop.StationCode, cancellationToken);
        if (valid is null || !valid.Contains(platform))
            return JourneysErrors.PlatformUnknown(stop.StationCode, platform);

        var (from, to) = PlatformRules.Window(stop);
        var others = await reader.GetPlatformUseAsync(stop.StationCode, platform, from, to, j.Id, cancellationToken);
        var conflicts = others.Select(o => new PlatformConflict(o.JourneyId, o.TrainNumber, o.DestinationStationCode, o.From, o.To)).ToList();
        return new PlatformCheck(stop.StationCode, platform, stop.PlatformCode, conflicts.Count == 0, conflicts);
    }
}

public sealed record ChangePlatformCommand(Guid JourneyId, int StopSequence, string Platform, string ReasonCode, string? Note, bool ConfirmOverlap, int? ExpectedVersion)
    : ICommand<OpsActionResult>;

public sealed class ChangePlatformCommandHandler(
    JourneyActions actions, IOpsJourneyReader reader, INetworkStations stations, ICurrentUser user)
    : ICommandHandler<ChangePlatformCommand, OpsActionResult>
{
    public async Task<Result<OpsActionResult>> Handle(ChangePlatformCommand command, CancellationToken cancellationToken)
    {
        var reason = JourneyActions.Reason(ReasonCatalog.Platform, command.ReasonCode, command.Note, out var error);
        if (error is not null)
            return error;

        var j = await reader.GetAsync(command.JourneyId, cancellationToken);
        if (j is null)
            return JourneysErrors.JourneyNotFound(command.JourneyId);

        var stop = j.Stops.FirstOrDefault(s => s.Sequence == command.StopSequence);
        if (stop is null)
            return JourneysErrors.StopNotFound(command.StopSequence);
        if (stop.Kind == JourneyStopKind.Passing)
            return JourneysErrors.StopHasNoPlatform;
        if (stop.State is JourneyStopState.Departed or JourneyStopState.Cancelled)
            return JourneysErrors.CannotTerminateThere;

        var platform = command.Platform.Trim();
        if (string.Equals(stop.PlatformCode, platform, StringComparison.Ordinal))
            return JourneysErrors.PlatformUnchanged;

        var valid = await stations.GetPlatformsAsync(stop.StationCode, cancellationToken);
        if (valid is null || !valid.Contains(platform))
            return JourneysErrors.PlatformUnknown(stop.StationCode, platform);

        // The controller may accept an overlap, but never by accident: the first try is refused and says who is there.
        if (!command.ConfirmOverlap)
        {
            var (from, to) = PlatformRules.Window(stop);
            var others = await reader.GetPlatformUseAsync(stop.StationCode, platform, from, to, j.Id, cancellationToken);
            if (others.Count > 0)
            {
                var first = others[0];
                return JourneysErrors.PlatformInUse(
                    $"Platform {platform} is in use at {first.From:HH:mm}: train {first.TrainNumber} is there. Choose another platform or confirm the overlap.");
            }
        }

        return await actions.RunAsync(command.JourneyId, command.ExpectedVersion, "journey.platform.change", reason,
            (journey, now) => journey.ChangePlatform(command.StopSequence, platform, user.UserId ?? "unknown", now),
            new { stop.StationCode, From = stop.PlatformCode, To = platform, command.ConfirmOverlap }, cancellationToken);
    }
}
