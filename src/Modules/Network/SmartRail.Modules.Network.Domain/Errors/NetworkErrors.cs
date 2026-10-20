using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Domain.Errors;

// Error codes are "Network.<PascalCaseReason>".
public static class NetworkErrors
{
    public static Error InvalidStationCode(string code) =>
        Error.Validation("Network.InvalidStationCode", $"Station code '{code}' must be 3 to 5 upper-case letters.");

    public static Error InvalidName =>
        Error.Validation("Network.InvalidName", "A name is required and must be at most 200 characters.");

    public static Error UnknownFacility(string facility) =>
        Error.Validation("Network.UnknownFacility", $"Facility '{facility}' is not a known facility.");

    public static Error InvalidPlatformCode(string code) =>
        Error.Validation("Network.InvalidPlatformCode", $"Platform code '{code}' must be 1 to 3 letters or digits.");

    public static Error DuplicatePlatform(string code) =>
        Error.Conflict("Network.DuplicatePlatform", $"The station already has a platform '{code}'.");

    public static Error DefaultLanguageTranslation(string languageCode) =>
        Error.Validation("Network.DefaultLanguageTranslation",
            $"'{languageCode}' is the default language; its name lives on the station and is never translated.");

    public static Error DuplicateTranslation(string languageCode) =>
        Error.Conflict("Network.DuplicateTranslation", $"The station already has a '{languageCode}' translation.");

    public static Error InvalidRouteCode(string code) =>
        Error.Validation("Network.InvalidRouteCode", $"Route code '{code}' must be 2 to 10 upper-case letters.");

    public static Error RouteNeedsTwoStops =>
        Error.Validation("Network.RouteNeedsTwoStops", "A route needs at least two stops.");

    public static Error RouteSequenceInvalid =>
        Error.Validation("Network.RouteSequenceInvalid", "Route stops must be numbered 1, 2, 3 ... with no gaps.");

    public static Error RouteStationRepeated =>
        Error.Validation("Network.RouteStationRepeated", "A station can appear only once on a route.");

    public static Error RouteTimeNegative =>
        Error.Validation("Network.RouteTimeNegative", "Running time and dwell time cannot be negative.");

    public static Error RouteFirstStopTime =>
        Error.Validation("Network.RouteFirstStopTime", "The first stop has 0 minutes from the previous stop.");

    public static Error StationNotFound(string code) =>
        Error.NotFound("Network.StationNotFound", $"Station '{code}' was not found.");

    public static Error RouteNotFound(string code) =>
        Error.NotFound("Network.RouteNotFound", $"Route '{code}' was not found.");

    public static Error SearchTooShort =>
        Error.Validation("Network.SearchTooShort", "A search needs at least 2 characters.");

    // Timetable
    public static Error NoTimetableForDate(DateOnly date) =>
        Error.NotFound("Network.NoTimetableForDate", $"There is no timetable for {date:yyyy-MM-dd}.");

    public static Error ServiceNotFound(Guid id) =>
        Error.NotFound("Network.ServiceNotFound", $"Service '{id}' was not found.");

    public static Error ServiceNotRunning(Guid id, DateOnly date) =>
        Error.NotFound("Network.ServiceNotRunning", $"Service '{id}' does not run on {date:yyyy-MM-dd}.");

    public static Error InvalidDate =>
        Error.Validation("Network.InvalidDate", "The date must be written yyyy-MM-dd.");

    public static Error InvalidTimetableKind =>
        Error.Validation("Network.InvalidTimetableKind", "The kind must be 'departures' or 'arrivals'.");

    public static Error InvalidTrainNumber =>
        Error.Validation("Network.InvalidTrainNumber", "A train number is 1 to 10 letters or digits.");

    public static Error InvalidCalendarCode =>
        Error.Validation("Network.InvalidCalendarCode", "A calendar code is 2 to 20 upper-case letters.");

    public static Error CalendarRangeInvalid =>
        Error.Validation("Network.CalendarRangeInvalid", "A calendar must run on at least one weekday and its end date cannot be before its start date.");

    public static Error DuplicateCalendarException(DateOnly date) =>
        Error.Conflict("Network.DuplicateCalendarException", $"The calendar already has an exception on {date:yyyy-MM-dd}.");

    public static Error ServiceNeedsTwoStops =>
        Error.Validation("Network.ServiceNeedsTwoStops", "A service needs at least two stops.");

    public static Error ServiceSequenceInvalid =>
        Error.Validation("Network.ServiceSequenceInvalid", "Service stops must be numbered 1, 2, 3 ... with no gaps.");

    public static Error ServiceStationRepeated =>
        Error.Validation("Network.ServiceStationRepeated", "A station can appear only once in a service.");

    public static Error ServiceStopsNotOnRoute =>
        Error.Validation("Network.ServiceStopsNotOnRoute", "The stops must follow the route in order, forward or reverse.");

    public static Error ServiceTimeOutOfRange =>
        Error.Validation("Network.ServiceTimeOutOfRange", "Stop times are 0 to 2879 minutes after midnight of the service date.");

    public static Error ServiceTimesInvalid =>
        Error.Validation("Network.ServiceTimesInvalid", "Times cannot go backwards, the first stop has no arrival and the last has no departure.");

    public static Error ServiceEndpointPassing =>
        Error.Validation("Network.ServiceEndpointPassing", "The first and last stop of a service cannot be a passing point.");

    public static Error VersionNotEditable(string status) =>
        Error.Conflict("Network.VersionNotEditable", $"A {status} timetable version cannot be changed. Create a new draft.");

    public static Error VersionWrongState(string expected, string actual) =>
        Error.Conflict("Network.VersionWrongState", $"The version must be {expected} but is {actual}.");

    public static Error AuthorCannotApprove =>
        Error.Forbidden("Network.AuthorCannotApprove", "The author of a timetable version cannot approve it.");

    public static Error VersionHasNoServices =>
        Error.Validation("Network.VersionHasNoServices", "A version with no services cannot be published.");

    public static Error EffectiveDateTaken(DateOnly date) =>
        Error.Conflict("Network.EffectiveDateTaken", $"Another version already takes effect on {date:yyyy-MM-dd}.");

    public static Error VersionNotFound(Guid id) =>
        Error.NotFound("Network.VersionNotFound", $"Timetable version '{id}' was not found.");

    public static Error ServiceNotInVersion(Guid id) =>
        Error.NotFound("Network.ServiceNotInVersion", $"Service '{id}' is not in this timetable version.");

    public static Error TrainNumberTaken(string trainNumber) =>
        Error.Conflict("Network.TrainNumberTaken", $"Train number '{trainNumber}' is already used in this version.");

    public static Error CalendarNotFound(string code) =>
        Error.NotFound("Network.CalendarNotFound", $"Calendar '{code}' was not found.");

    public static Error EffectiveDateInPast =>
        Error.Validation("Network.EffectiveDateInPast", "A new version takes effect after today, so journeys already running are not touched.");

    public static Error NothingToBaseOn =>
        Error.Conflict("Network.NothingToBaseOn", "There is no published version to start a draft from.");

    public static Error DraftAlreadyOpen =>
        Error.Conflict("Network.DraftAlreadyOpen", "A draft or a version in review already exists. Finish or publish it first.");

    public static Error StopTimesMismatch =>
        Error.Validation("Network.StopTimesMismatch", "Give a time for every stop of the service, in order.");
}
