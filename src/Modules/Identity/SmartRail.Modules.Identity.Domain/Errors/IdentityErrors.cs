using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Identity.Domain.Errors;

// Error codes are "Identity.<PascalCaseReason>".
public static class IdentityErrors
{
    public static readonly Error AccountExists =
        Error.Conflict("Identity.AccountExists", "An account with this email already exists. Try logging in.");

    public static readonly Error AccountNotFound =
        Error.NotFound("Identity.AccountNotFound", "This account was not found.");

    public static readonly Error DirectoryUnavailable =
        Error.Failure("Identity.DirectoryUnavailable", "We could not reach the account service. Please try again in a moment.");

    public static readonly Error TermsNotAccepted =
        Error.Validation("Identity.TermsNotAccepted", "You need to agree to the terms and the privacy notice.");

    // ---- staff administration ----

    public static readonly Error StaffNotFound =
        Error.NotFound("Identity.StaffNotFound", "This staff member was not found.");

    public static readonly Error NotAStaffRole =
        Error.Validation("Identity.NotAStaffRole", "Choose one of the staff roles: driver, inspector, controller, station_master, scheduler or admin.");

    public static readonly Error StationRequired =
        Error.Validation("Identity.StationRequired", "A station master needs at least one station.");

    public static readonly Error CannotChangeOwnAccess =
        Error.Conflict("Identity.CannotChangeOwnAccess", "You cannot change or disable your own access. Ask another administrator.");

    public static readonly Error LastAdministrator =
        Error.Conflict("Identity.LastAdministrator", "This is the last administrator. Give someone else the administrator role first.");

    public static readonly Error ReasonRequired =
        Error.Validation("Identity.ReasonRequired", "A reason is required.");

    // ---- devices ----

    public static readonly Error DeviceNotFound =
        Error.NotFound("Identity.DeviceNotFound", "This device was not found.");

    public static readonly Error DeviceNameInvalid =
        Error.Validation("Identity.DeviceNameInvalid", "A device name is required and must be at most 80 characters.");

    public static readonly Error DeviceStationInvalid =
        Error.Validation("Identity.DeviceStationInvalid", "Choose the station the device is at.");

    public static readonly Error DeviceTypeInvalid =
        Error.Validation("Identity.DeviceTypeInvalid", "The device type is PlatformDisplay, ConcourseBoard, GateScanner or Terminal.");

    public static readonly Error DeviceAlreadyRevoked =
        Error.Conflict("Identity.DeviceAlreadyRevoked", "This device is already revoked.");

    // Same answer for a wrong, spent or expired code, so a guess teaches nothing.
    // ---- settings ----

    public static Error SettingInvalid(string name, string rule) =>
        Error.Validation("Identity.SettingInvalid", $"{name}: {rule}");
}
