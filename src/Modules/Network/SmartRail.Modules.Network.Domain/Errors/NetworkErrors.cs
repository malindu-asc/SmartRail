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
}
