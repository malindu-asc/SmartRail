using System.Text.RegularExpressions;
using SmartRail.Modules.Journeys.Domain.Crew;
using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.Modules.Journeys.Domain.Fleet;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Domain.Journeys;

// One run of a service on one service date. The journey id is the identity of a train run (REQ-LIVE-019).
// Everything else (vehicle, driver, delay, platform) attaches to it.
public sealed partial class Journey : AuditableEntity
{
    private readonly List<JourneyStop> _stops = [];
    private readonly List<VehicleAssignment> _vehicleAssignments = [];
    private readonly List<DriverAssignment> _driverAssignments = [];

    private Journey() { }

    private Journey(Guid id) : base(id) { }

    public Guid? ServiceId { get; private set; }
    public Guid? TimetableVersionId { get; private set; }
    public DateOnly ServiceDate { get; private set; }
    public string TrainNumber { get; private set; } = string.Empty;
    public string RouteCode { get; private set; } = string.Empty;
    public string OriginStationCode { get; private set; } = string.Empty;
    public string DestinationStationCode { get; private set; } = string.Empty;
    public JourneyCreationOrigin CreationOrigin { get; private set; }
    public string? ManualReason { get; private set; }

    // Fixed when the journey is created; there is no way to change them.
    public DateTimeOffset ScheduledDepartureUtc { get; private set; }
    public DateTimeOffset ScheduledArrivalUtc { get; private set; }

    public JourneyLifecycle Lifecycle { get; private set; } = JourneyLifecycle.Planned;
    public RunningSubState? RunningSubState { get; private set; }
    public int? CurrentStopSequence { get; private set; }
    public int? DelayMinutes { get; private set; }
    public PunctualityBand? PunctualityBand { get; private set; }
    public JourneyConfidence Confidence { get; private set; } = JourneyConfidence.Scheduled;
    public DateTimeOffset? LastAcceptedUpdateAt { get; private set; }
    public DateTimeOffset? InterruptedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public int? EndedAtStopSequence { get; private set; }
    public string? CancelReasonCode { get; private set; }

    // Why the journey was interrupted or ended early (terminated short or emergency), and the controller's note for passengers.
    public string? EndReasonCode { get; private set; }
    public string? ControllerNote { get; private set; }

    // Why the train is late, as the driver last said it (signal_fault, door_fault, crew, passenger_incident, track, other).
    public string? DelayReasonCode { get; private set; }
    public DateTimeOffset? DelayReasonAt { get; private set; }

    // Optimistic concurrency (REQ-OPS-010): the domain bumps it on every change.
    public int Version { get; private set; }

    public IReadOnlyList<JourneyStop> Stops => _stops;
    public IReadOnlyList<VehicleAssignment> VehicleAssignments => _vehicleAssignments;
    public IReadOnlyList<DriverAssignment> DriverAssignments => _driverAssignments;

    public static Result<Journey> Create(
        Guid? serviceId,
        Guid? timetableVersionId,
        DateOnly serviceDate,
        string trainNumber,
        string routeCode,
        JourneyCreationOrigin origin,
        string? manualReason,
        IReadOnlyList<JourneyStopDraft> stops,
        Guid? id = null)
    {
        if (!TrainNumberPattern().IsMatch(trainNumber))
            return JourneysErrors.InvalidTrainNumber;

        if (origin == JourneyCreationOrigin.Manual && string.IsNullOrWhiteSpace(manualReason))
            return JourneysErrors.ReasonRequired;

        if (stops.Count < 2)
            return JourneysErrors.InvalidStops;

        for (var i = 0; i < stops.Count; i++)
        {
            if (stops[i].Sequence != i + 1)
                return JourneysErrors.InvalidStops;
        }

        if (stops[0].Kind == JourneyStopKind.Passing || stops[^1].Kind == JourneyStopKind.Passing)
            return JourneysErrors.PassingEndpoint;

        var times = ValidateTimes(stops);
        if (times.IsFailure)
            return times.Error;

        var journey = new Journey(id ?? Guid.NewGuid())
        {
            ServiceId = serviceId,
            TimetableVersionId = timetableVersionId,
            ServiceDate = serviceDate,
            TrainNumber = trainNumber,
            RouteCode = routeCode,
            OriginStationCode = stops[0].StationCode,
            DestinationStationCode = stops[^1].StationCode,
            CreationOrigin = origin,
            ManualReason = manualReason?.Trim(),
            ScheduledDepartureUtc = stops[0].ScheduledDepartureUtc!.Value,
            ScheduledArrivalUtc = stops[^1].ScheduledArrivalUtc!.Value
        };

        journey._stops.AddRange(stops.Select(s => new JourneyStop(journey.Id, s)));
        return journey;
    }

    private static Result ValidateTimes(IReadOnlyList<JourneyStopDraft> stops)
    {
        var last = stops.Count - 1;
        DateTimeOffset? previous = null;

        for (var i = 0; i <= last; i++)
        {
            var stop = stops[i];

            if (i == 0 && (stop.ScheduledArrivalUtc is not null || stop.ScheduledDepartureUtc is null))
                return JourneysErrors.StopTimesInvalid;

            if (i == last && (stop.ScheduledDepartureUtc is not null || stop.ScheduledArrivalUtc is null))
                return JourneysErrors.StopTimesInvalid;

            if (i > 0 && i < last && (stop.ScheduledArrivalUtc is null || stop.ScheduledDepartureUtc is null))
                return JourneysErrors.StopTimesInvalid;

            if (stop.ScheduledArrivalUtc is { } arrival && stop.ScheduledDepartureUtc is { } departure && arrival > departure)
                return JourneysErrors.StopTimesInvalid;

            var firstTime = stop.ScheduledArrivalUtc ?? stop.ScheduledDepartureUtc!.Value;
            if (previous is not null && firstTime < previous)
                return JourneysErrors.StopTimesInvalid;

            previous = stop.ScheduledDepartureUtc ?? stop.ScheduledArrivalUtc;
        }

        return Result.Success();
    }

    // ---- lifecycle ----

    // The one door every lifecycle change goes through (later specs call it for Running, Interrupted and so on).
    public Result TransitionTo(JourneyLifecycle next)
    {
        if (!JourneyLifecycleRules.IsAllowed(Lifecycle, next))
            return JourneysErrors.IllegalTransition(Lifecycle.ToString(), next.ToString());

        Lifecycle = next;
        if (next != JourneyLifecycle.Running)
            RunningSubState = null;

        Touch();
        return Result.Success();
    }

    public Result Cancel(string reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
            return JourneysErrors.ReasonRequired;

        var moved = TransitionTo(JourneyLifecycle.Cancelled);
        if (moved.IsFailure)
            return moved;

        CancelReasonCode = reasonCode.Trim();
        return Result.Success();
    }

    // Only before the scheduled departure (state-models.md: Cancelled to Planned, with a reason).
    public Result Reinstate(DateTimeOffset now, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return JourneysErrors.ReasonRequired;

        if (Lifecycle != JourneyLifecycle.Cancelled)
            return JourneysErrors.IllegalTransition(Lifecycle.ToString(), nameof(JourneyLifecycle.Planned));

        if (now >= ScheduledDepartureUtc)
            return JourneysErrors.ReinstateTooLate;

        var moved = TransitionTo(JourneyLifecycle.Planned);
        if (moved.IsFailure)
            return moved;

        CancelReasonCode = null;
        RefreshReadiness();
        return Result.Success();
    }

    // ---- vehicle assignment (time-ranged, REQ-LIVE-010) ----

    public bool HasVehicle => _vehicleAssignments.Any(a => a.Covers(ScheduledDepartureUtc));

    public bool HasDriver => _driverAssignments.Any(a => a.Covers(ScheduledDepartureUtc));

    public Result AssignVehicle(Guid vehicleId, DateTimeOffset from, DateTimeOffset? to, string assignedBy, string? reason = null)
    {
        var guard = GuardAssignment(from, to);
        if (guard.IsFailure)
            return guard;

        if (_vehicleAssignments.Any(a => a.Overlaps(from, to)))
            return JourneysErrors.AssignmentOverlaps;

        _vehicleAssignments.Add(new VehicleAssignment(Id, vehicleId, from, to, assignedBy, reason));
        RefreshReadiness();
        Touch();
        return Result.Success();
    }

    // A substitution keeps the journey id and its history: the old row closes, the new one opens (REQ-LIVE-010).
    public Result ReplaceVehicle(Guid newVehicleId, DateTimeOffset at, string assignedBy, string? reason = null)
    {
        var current = _vehicleAssignments.FirstOrDefault(a => a.Covers(at));
        if (current is null)
            return JourneysErrors.NoAssignmentToChange;

        var closed = current.Close(at);
        if (closed.IsFailure)
            return closed;

        _vehicleAssignments.Add(new VehicleAssignment(Id, newVehicleId, at, null, assignedBy, reason));
        RefreshReadiness();
        Touch();
        return Result.Success();
    }

    public Result UnassignVehicle(DateTimeOffset at)
    {
        var current = _vehicleAssignments.FirstOrDefault(a => a.Covers(at));
        if (current is null)
            return JourneysErrors.NoAssignmentToChange;

        var closed = current.Close(at);
        if (closed.IsFailure)
            return closed;

        RefreshReadiness();
        Touch();
        return Result.Success();
    }

    // ---- driver assignment (the outgoing duty closes before the incoming opens, REQ-DRV-009) ----

    public Result AssignDriver(Guid dutyId, DateTimeOffset from, DateTimeOffset? to, string assignedBy)
    {
        var guard = GuardAssignment(from, to);
        if (guard.IsFailure)
            return guard;

        if (_driverAssignments.Any(a => a.Overlaps(from, to)))
            return JourneysErrors.AssignmentOverlaps;

        _driverAssignments.Add(new DriverAssignment(Id, dutyId, from, to, assignedBy));
        RefreshReadiness();
        Touch();
        return Result.Success();
    }

    public Result UnassignDriver(DateTimeOffset at)
    {
        var current = _driverAssignments.FirstOrDefault(a => a.Covers(at));
        if (current is null)
            return JourneysErrors.NoAssignmentToChange;

        var closed = current.Close(at);
        if (closed.IsFailure)
            return closed;

        RefreshReadiness();
        Touch();
        return Result.Success();
    }

    private Result GuardAssignment(DateTimeOffset from, DateTimeOffset? to)
    {
        if (JourneyLifecycleRules.IsFinal(Lifecycle) || Lifecycle == JourneyLifecycle.Cancelled)
            return JourneysErrors.JourneyEnded(Lifecycle.ToString());

        return to is not null && to <= from ? JourneysErrors.InvalidPeriod : Result.Success();
    }

    // Planned and Ready follow the assignments: both a vehicle and a driver means Ready (state-models.md).
    private void RefreshReadiness()
    {
        if (Lifecycle == JourneyLifecycle.Planned && HasVehicle && HasDriver)
            Lifecycle = JourneyLifecycle.Ready;
        else if (Lifecycle == JourneyLifecycle.Ready && !(HasVehicle && HasDriver))
            Lifecycle = JourneyLifecycle.Planned;
    }

    private void Touch() => Version++;

    [GeneratedRegex("^[A-Za-z0-9]{1,10}$")]
    private static partial Regex TrainNumberPattern();
}
