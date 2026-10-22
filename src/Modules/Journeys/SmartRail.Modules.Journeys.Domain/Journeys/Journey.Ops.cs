using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Domain.Journeys;

// What a controller does to a journey (REQ-OPS-004): cancel, terminate short, interrupt and resume, reinstate, emergency termination.
// Every action needs a reason; the audit row is written by the handler in the same transaction.
public sealed partial class Journey
{
    // A journey that has not left can be cancelled, and so can an interrupted one. A running train is interrupted or terminated
    // short instead (state-models.md 1a).
    public Result CancelWithNote(string reasonCode, string? note)
    {
        var cancelled = Cancel(reasonCode);
        if (cancelled.IsFailure)
            return cancelled;

        ControllerNote = Clean(note);
        MarkRemainingStopsCancelled(0);
        RaiseDomainEvent(new JourneyCancelledDomainEvent(Id, reasonCode.Trim()));
        return Result.Success();
    }

    // The train stops its run at a stop it has not passed. Later stops are cancelled and the journey is final.
    public Result TerminateShort(int atStopSequence, string reasonCode, string? note, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
            return JourneysErrors.ReasonRequired;

        var stop = _stops.FirstOrDefault(s => s.Sequence == atStopSequence);
        if (stop is null)
            return JourneysErrors.StopNotFound(atStopSequence);

        var last = _stops.Max(s => s.Sequence);
        var behind = CurrentStopSequence is { } current && atStopSequence < current;
        if (atStopSequence >= last || behind || stop.Kind == JourneyStopKind.Passing)
            return JourneysErrors.CannotTerminateThere;

        var moved = TransitionTo(JourneyLifecycle.TerminatedShort);
        if (moved.IsFailure)
            return moved;

        EndedAt = now;
        EndedAtStopSequence = atStopSequence;
        EndReasonCode = reasonCode.Trim();
        ControllerNote = Clean(note);
        Confidence = JourneyConfidence.Confirmed;
        MarkRemainingStopsCancelled(atStopSequence);
        RaiseDomainEvent(new JourneyTerminatedShortDomainEvent(Id, atStopSequence, EndReasonCode));
        return Result.Success();
    }

    // A train that cannot go on for now (a fault, a blocked line). It stays on the board but with no estimate.
    public Result Interrupt(string reasonCode, string? note, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
            return JourneysErrors.ReasonRequired;

        var moved = TransitionTo(JourneyLifecycle.Interrupted);
        if (moved.IsFailure)
            return moved;

        InterruptedAt = now;
        EndReasonCode = reasonCode.Trim();
        ControllerNote = Clean(note);

        // No honest ETA while interrupted (REQ-LIVE-013): delay and estimates are cleared.
        DelayMinutes = null;
        PunctualityBand = null;
        foreach (var stop in _stops.Where(s => s.State == JourneyStopState.Pending))
            stop.ClearEstimate();

        RaiseDomainEvent(new JourneyInterruptedDomainEvent(Id, EndReasonCode));
        return Result.Success();
    }

    public Result Resume(DateTimeOffset now)
    {
        var moved = TransitionTo(JourneyLifecycle.Running);
        if (moved.IsFailure)
            return moved;

        InterruptedAt = null;
        EndReasonCode = null;
        RunningSubState = Journeys.RunningSubState.InSection;
        LastAcceptedUpdateAt = now;
        RaiseDomainEvent(new JourneyResumedDomainEvent(Id));
        return Result.Success();
    }

    // A major incident: the journey ends where it is, its ETA is unknown, and public speculation is blocked (REQ-OPS-008).
    public Result TerminateEmergency(string reasonCode, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
            return JourneysErrors.ReasonRequired;

        var moved = TransitionTo(JourneyLifecycle.TerminatedEmergency);
        if (moved.IsFailure)
            return moved;

        EndedAt = now;
        EndedAtStopSequence = CurrentStopSequence;
        EndReasonCode = reasonCode.Trim();
        Confidence = JourneyConfidence.Unknown;
        DelayMinutes = null;
        PunctualityBand = null;
        MarkRemainingStopsCancelled(CurrentStopSequence ?? 0);
        RaiseDomainEvent(new JourneyEmergencyTerminatedDomainEvent(Id, EndReasonCode));
        return Result.Success();
    }

    // Reinstating clears the note as well as the reason.
    public Result ReinstateWithReason(DateTimeOffset now, string reason)
    {
        var result = Reinstate(now, reason);
        if (result.IsFailure)
            return result;

        ControllerNote = null;
        foreach (var stop in _stops.Where(s => s.State == JourneyStopState.Cancelled))
            stop.RestorePending();

        RaiseDomainEvent(new JourneyReinstatedDomainEvent(Id));
        return Result.Success();
    }

    // ---- planning: put a unit or a driver on the journey, or take it off (REQ-OPS-004) ----

    // Before departure the new assignment simply replaces the old one for the whole journey. After departure the old one
    // closes now and the new one opens now, so the history shows who had the train when.
    public Result PutUnit(Guid vehicleId, DateTimeOffset now, string assignedBy, string? reason)
    {
        var guard = GuardAssignment(now < ScheduledDepartureUtc ? ScheduledDepartureUtc : now, ScheduledArrivalUtc);
        if (guard.IsFailure)
            return guard;

        if (now >= ScheduledDepartureUtc)
            return HasVehicle ? ReplaceVehicle(vehicleId, now, assignedBy, reason) : AssignVehicle(vehicleId, now, ScheduledArrivalUtc, assignedBy, reason);

        _vehicleAssignments.RemoveAll(a => a.ValidFrom >= ScheduledDepartureUtc && a.Covers(ScheduledDepartureUtc));
        return AssignVehicle(vehicleId, ScheduledDepartureUtc, ScheduledArrivalUtc, assignedBy, reason);
    }

    public Result PutDriver(Guid dutyId, DateTimeOffset now, string assignedBy)
    {
        var guard = GuardAssignment(now < ScheduledDepartureUtc ? ScheduledDepartureUtc : now, ScheduledArrivalUtc);
        if (guard.IsFailure)
            return guard;

        if (now >= ScheduledDepartureUtc)
        {
            if (HasDriver)
            {
                var closed = UnassignDriver(now);
                if (closed.IsFailure)
                    return closed;
            }

            return AssignDriver(dutyId, now, ScheduledArrivalUtc, assignedBy);
        }

        _driverAssignments.RemoveAll(a => a.ValidFrom >= ScheduledDepartureUtc && a.Covers(ScheduledDepartureUtc));
        return AssignDriver(dutyId, ScheduledDepartureUtc, ScheduledArrivalUtc, assignedBy);
    }

    public Result TakeOffUnit(DateTimeOffset now)
    {
        if (now >= ScheduledDepartureUtc)
            return UnassignVehicle(now);

        var removed = _vehicleAssignments.RemoveAll(a => a.ValidFrom >= ScheduledDepartureUtc && a.Covers(ScheduledDepartureUtc));
        if (removed == 0)
            return JourneysErrors.NoAssignmentToChange;

        RefreshReadiness();
        Touch();
        return Result.Success();
    }

    public Result TakeOffDriver(DateTimeOffset now)
    {
        if (now >= ScheduledDepartureUtc)
            return UnassignDriver(now);

        var removed = _driverAssignments.RemoveAll(a => a.ValidFrom >= ScheduledDepartureUtc && a.Covers(ScheduledDepartureUtc));
        if (removed == 0)
            return JourneysErrors.NoAssignmentToChange;

        RefreshReadiness();
        Touch();
        return Result.Success();
    }

    private void MarkRemainingStopsCancelled(int afterSequence)
    {
        foreach (var stop in _stops.Where(s => s.Sequence > afterSequence && s.State == JourneyStopState.Pending))
            stop.MarkCancelled();
    }

    private static string? Clean(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(note.Trim().Length, 300)];
}

public sealed record JourneyCancelledDomainEvent(Guid JourneyId, string ReasonCode) : DomainEvent;

public sealed record JourneyTerminatedShortDomainEvent(Guid JourneyId, int AtStopSequence, string ReasonCode) : DomainEvent;

public sealed record JourneyInterruptedDomainEvent(Guid JourneyId, string ReasonCode) : DomainEvent;

public sealed record JourneyResumedDomainEvent(Guid JourneyId) : DomainEvent;

public sealed record JourneyEmergencyTerminatedDomainEvent(Guid JourneyId, string ReasonCode) : DomainEvent;

public sealed record JourneyReinstatedDomainEvent(Guid JourneyId) : DomainEvent;
