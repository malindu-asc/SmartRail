namespace SmartRail.Api.Core.Security;

// The four roles of the platform. They are Keycloak realm roles and arrive in the flat "roles" claim.
// Do not add roles here without an agreed requirement.
public static class Roles
{
    public const string Passenger = "passenger";
    public const string Driver = "driver";
    public const string Controller = "controller";
    public const string Admin = "admin";

    public static readonly IReadOnlyList<string> All = [Passenger, Driver, Controller, Admin];
}

// Authorisation policy names used by endpoints (RequireAuthorization(Policies.Controller)).
public static class Policies
{
    public const string Driver = "Driver";
    public const string Controller = "Controller";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = [Driver, Controller, Admin];
}
