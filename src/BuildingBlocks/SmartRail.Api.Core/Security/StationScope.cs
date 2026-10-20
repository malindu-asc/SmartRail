using System.Security.Claims;

namespace SmartRail.Api.Core.Security;

// A station master works on their own stations only (the "stations" claim). Controllers, schedulers and administrators see the network.
public static class StationScope
{
    public sealed record Scope(string? Station, bool Forbidden);

    public static Scope Resolve(ClaimsPrincipal user, string? requested)
    {
        var roles = user.FindAll("roles").Select(c => c.Value).ToList();
        var wide = roles.Contains(Roles.Controller) || roles.Contains(Roles.Scheduler) || roles.Contains(Roles.Admin);
        if (wide || !roles.Contains(Roles.StationMaster))
            return new Scope(requested, false);

        var own = user.FindAll("stations").Select(c => c.Value.ToUpperInvariant()).ToList();
        var wanted = string.IsNullOrWhiteSpace(requested) ? own.FirstOrDefault() : requested.Trim().ToUpperInvariant();
        return wanted is not null && own.Contains(wanted) ? new Scope(wanted, false) : new Scope(null, true);
    }

    // True when the caller may see something at this station.
    public static bool Allows(ClaimsPrincipal user, string stationCode) => !Resolve(user, stationCode).Forbidden;
}
