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
}
