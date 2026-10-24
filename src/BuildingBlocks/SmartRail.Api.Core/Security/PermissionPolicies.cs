using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace SmartRail.Api.Core.Security;

public static class PermissionPolicies
{
    // One policy per permission, named after it. A caller passes when any of their roles holds the permission.
    public static AuthorizationBuilder AddPermissionPolicies(this AuthorizationBuilder builder)
    {
        foreach (var permission in Permissions.All)
        {
            var name = permission;
            builder.AddPolicy(name, policy => policy.RequireAssertion(context =>
                RolePermissions.Allows(context.User.FindAll("roles").Select(c => c.Value), name)));
        }

        return builder;
    }

    // endpoint.RequirePermission(Permissions.JourneyCancel)
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(permission);
}
