namespace SmartRail.Api.Core.Security;

// The roles of the platform. They are Keycloak realm roles and arrive in the flat "roles" claim.
// Do not add roles here without an agreed requirement (mvp-roles.md lists the seven that have accounts).
public static class Roles
{
    public const string Passenger = "passenger";
    public const string Driver = "driver";
    public const string Inspector = "inspector";

    // Network Controller; also holds the Incident Manager and Reporting Analyst bundles.
    public const string Controller = "controller";
    public const string StationMaster = "station_master";

    // Scheduler; also holds the Fleet Controller bundle.
    public const string Scheduler = "scheduler";

    // System Administrator: manages access and devices, holds no operational permission.
    public const string Admin = "admin";

    public static readonly IReadOnlyList<string> All = [Passenger, Driver, Inspector, Controller, StationMaster, Scheduler, Admin];

    // Roles that work in the staff consoles and the staff app (not the passenger app).
    public static readonly IReadOnlyList<string> Staff = [Driver, Inspector, Controller, StationMaster, Scheduler, Admin];
}

// Authorisation policy names used by the first endpoints (RequireAuthorization(Policies.Controller)).
// New endpoints ask for a permission instead (see Permissions).
public static class Policies
{
    public const string Driver = "Driver";
    public const string Controller = "Controller";
    public const string Scheduler = "Scheduler";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = [Driver, Controller, Scheduler, Admin];
}
