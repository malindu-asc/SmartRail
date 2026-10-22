using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Domain.Errors;

// Error codes are "Journeys.<PascalCaseReason>".
public static class JourneysErrors
{
    public static Error JourneyNotFound(Guid id) =>
        Error.NotFound("Journeys.JourneyNotFound", $"Journey '{id}' was not found.");

    public static Error SameStation =>
        Error.Validation("Journeys.SameStation", "Choose two different stations.");

    public static Error StationNotFound(string code) =>
        Error.NotFound("Journeys.StationNotFound", $"Station '{code}' was not found.");

    public static Error RouteNotFound(string code) =>
        Error.NotFound("Journeys.RouteNotFound", $"Route '{code}' was not found.");

    public static Error IllegalTransition(string from, string to) =>
        Error.Conflict("Journeys.IllegalTransition", $"A journey cannot go from {from} to {to}.");

    public static Error JourneyEnded(string lifecycle) =>
        Error.Conflict("Journeys.JourneyEnded", $"The journey is {lifecycle} and can no longer be changed.");

    public static Error ReinstateTooLate =>
        Error.Conflict("Journeys.ReinstateTooLate", "A cancelled journey can be reinstated only before its scheduled departure.");

    public static Error ReasonRequired =>
        Error.Validation("Journeys.ReasonRequired", "A reason is required.");

    public static Error InvalidTrainNumber =>
        Error.Validation("Journeys.InvalidTrainNumber", "A train number is 1 to 10 letters or digits.");

    public static Error InvalidStops =>
        Error.Validation("Journeys.InvalidStops", "A journey needs at least two stops, numbered 1, 2, 3 ... with no gaps.");

    public static Error StopTimesInvalid =>
        Error.Validation("Journeys.StopTimesInvalid", "Stop times cannot go backwards, the first stop has no arrival and the last has no departure.");

    public static Error StopsNotOnRoute =>
        Error.Validation("Journeys.StopsNotOnRoute", "The stops must follow the route in order, forward or reverse.");

    public static Error PassingEndpoint =>
        Error.Validation("Journeys.PassingEndpoint", "The first and last stop cannot be a passing point.");

    public static Error AssignmentOverlaps =>
        Error.Conflict("Journeys.AssignmentOverlaps", "The journey already has an assignment in that period.");

    public static Error NoAssignmentToChange =>
        Error.Conflict("Journeys.NoAssignmentToChange", "The journey has no assignment in force at that time.");

    public static Error InvalidPeriod =>
        Error.Validation("Journeys.InvalidPeriod", "The end of a period must be after its start.");

    public static Error InvalidVehicleCode =>
        Error.Validation("Journeys.InvalidVehicleCode", "A vehicle code is 2 to 30 upper-case letters, digits or hyphens.");

    public static Error InvalidName =>
        Error.Validation("Journeys.InvalidName", "A name is required and must be at most 100 characters.");

    public static Error InvalidDriver =>
        Error.Validation("Journeys.InvalidDriver", "A driver subject is required.");

    public static Error JourneyNotReady =>
        Error.Conflict("Journeys.JourneyNotReady", "The journey has no vehicle and driver yet, so it cannot be running.");

    public static Error StopNotFound(int sequence) =>
        Error.Validation("Journeys.StopNotFound", $"The journey has no stop number {sequence}.");

    public static Error InvalidCount =>
        Error.Validation("Journeys.InvalidCount", "The count must be between 1 and 50.");

    // ---- controller actions ----

    public static Error VersionConflict(int current) =>
        Error.Conflict("Journeys.VersionConflict", $"Someone else changed this journey (now version {current}). Reload it and try again.");

    public static Error ReasonCodeUnknown(string code) =>
        Error.Validation("Journeys.ReasonCodeUnknown", $"'{code}' is not a reason code for this action.");

    public static Error CannotTerminateThere =>
        Error.Validation("Journeys.CannotTerminateThere", "Choose a stop the train has not passed yet, and not the last stop.");

    public static Error PlatformUnknown(string station, string platform) =>
        Error.Validation("Journeys.PlatformUnknown", $"Station {station} has no platform '{platform}'.");

    public static Error PlatformUnchanged =>
        Error.Validation("Journeys.PlatformUnchanged", "The journey is already at that platform.");

    public static Error PlatformInUse(string detail) =>
        Error.Conflict("Journeys.PlatformInUse", detail);

    public static Error StopHasNoPlatform =>
        Error.Validation("Journeys.StopHasNoPlatform", "A passing point has no platform.");

    public static Error VehicleNotFound(Guid id) =>
        Error.NotFound("Journeys.VehicleNotFound", $"Unit '{id}' was not found.");

    public static Error DutyNotFound(Guid id) =>
        Error.NotFound("Journeys.DutyNotFound", $"Duty '{id}' was not found.");

    public static Error VehicleUnavailable =>
        Error.Conflict("Journeys.VehicleUnavailable", "That unit is out of service at that time.");

    public static Error VehicleBusy =>
        Error.Conflict("Journeys.VehicleBusy", "That unit is already on another journey at that time.");

    public static Error DutyDoesNotCover =>
        Error.Conflict("Journeys.DutyDoesNotCover", "The driver's duty does not cover the whole journey.");

    public static Error DriverBusy =>
        Error.Conflict("Journeys.DriverBusy", "That driver is already on another journey at that time.");

    public static Error DriverNotRested(int minutes) =>
        Error.Conflict("Journeys.DriverNotRested", $"The driver needs at least {minutes} minutes of rest before this journey.");

    public static Error NothingToAssign =>
        Error.Validation("Journeys.NothingToAssign", "Choose a unit, a driver duty, or both.");

    public static Error UnavailabilityNotFound(Guid id) =>
        Error.NotFound("Journeys.UnavailabilityNotFound", $"Out-of-service period '{id}' was not found.");

    public static Error DriverUnknown =>
        Error.Validation("Journeys.DriverUnknown", "That person is not a driver.");

    public static Error DutyAlreadySignedOn =>
        Error.Conflict("Journeys.DutyAlreadySignedOn", "You are already signed on to this duty, or it has ended.");

    public static Error DutyNotCurrent =>
        Error.Conflict("Journeys.DutyNotCurrent", "This duty is not current. You can sign on from 30 minutes before it starts until it ends.");

    public static Error DutyNotSignedOn =>
        Error.Conflict("Journeys.DutyNotSignedOn", "Sign on to your duty first.");

    public static Error NotYourJourney =>
        Error.NotFound("Journeys.NotYourJourney", "That journey is not on your duty.");

    public static Error DelayReasonUnknown =>
        Error.Validation("Journeys.DelayReasonUnknown", "Choose one of the delay reasons: signal fault, door fault, crew, passenger incident, track or other.");

    public static Error StopDoesNotMatch(string expected) =>
        Error.Conflict("Journeys.StopDoesNotMatch", $"That is not the stop the train is at. {expected}");
}
