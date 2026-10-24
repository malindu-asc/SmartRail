using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartRail.Api.Core.Security;

namespace SmartRail.Api.Authentication;

public static class AuthenticationExtensions
{
    // A placeholder that never resolves. With no identity provider configured every protected
    // endpoint answers 401 and public ones keep working, instead of failing with 500.
    private const string UnconfiguredAuthority = "http://identity-not-configured.invalid";

    public static IServiceCollection AddSmartRailAuthentication(this IServiceCollection services)
    {
        // Half configured is a startup error; nothing configured is allowed and answers 401.
        services
            .AddOptions<AuthenticationOptions>()
            .BindConfiguration(AuthenticationOptions.SectionName)
            .Validate(
                o => string.IsNullOrWhiteSpace(o.MetadataAddress) || !string.IsNullOrWhiteSpace(o.Authority),
                "Authentication is half configured: Authentication:MetadataAddress needs Authentication:Authority.")
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer()
            .AddScheme<AuthenticationSchemeOptions, FeedKeyAuthenticationHandler>(FeedSourceAuth.Scheme, _ => { });

        // Bound lazily from IConfiguration, so environment variables and host overrides always apply.
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration>((jwt, configuration) =>
            {
                var options = configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>()
                              ?? new AuthenticationOptions();

                var hasAuthority = !string.IsNullOrWhiteSpace(options.Authority);

                jwt.Authority = hasAuthority ? options.Authority : UnconfiguredAuthority;
                if (!string.IsNullOrWhiteSpace(options.MetadataAddress))
                    jwt.MetadataAddress = options.MetadataAddress;

                jwt.Audience = options.Audience;
                jwt.RequireHttpsMetadata = options.RequireHttpsMetadata && hasAuthority;
                jwt.MapInboundClaims = false;

                // The issuer is explicit: a token's "iss" is the address Keycloak was reached at.
                jwt.TokenValidationParameters.ValidIssuer = hasAuthority ? options.Authority : UnconfiguredAuthority;
                jwt.TokenValidationParameters.ValidAudience = options.Audience;
                jwt.TokenValidationParameters.ValidateIssuer = true;
                jwt.TokenValidationParameters.ValidateAudience = true;
                jwt.TokenValidationParameters.ValidateLifetime = true;
                jwt.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds);
                jwt.TokenValidationParameters.RoleClaimType = "roles";
                jwt.TokenValidationParameters.NameClaimType = "preferred_username";

                // Responses never say why a token was refused. The reason goes to the log and a metric,
                // and never includes the token or the Authorization header.
                jwt.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        var reason = context.AuthenticateFailure is null ? "no_token" : "invalid_token";
                        Reject(context.HttpContext, "Authentication refused", reason);
                        context.Response.Headers.WWWAuthenticate = "Bearer";
                        await WriteProblem(context.HttpContext, StatusCodes.Status401Unauthorized, "Unauthorized");
                    },
                    OnForbidden = async context =>
                    {
                        Reject(context.HttpContext, "Authorisation refused", "forbidden");
                        await WriteProblem(context.HttpContext, StatusCodes.Status403Forbidden, "Forbidden");
                    }
                };
            });

        services
            .AddAuthorizationBuilder()
            // Secure by default: every endpoint needs a signed-in user unless it opts out with AllowAnonymous.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.Driver, policy => policy.RequireRole(Roles.Driver))
            .AddPolicy(Policies.Controller, policy => policy.RequireRole(Roles.Controller))
            .AddPolicy(Policies.Scheduler, policy => policy.RequireRole(Roles.Scheduler))
            .AddPolicy(Policies.Admin, policy => policy.RequireRole(Roles.Admin))
            // One policy for every permission in the matrix (permission-matrix.md).
            .AddPermissionPolicies()
            // Machines that send live data: a source key, never a user token.
            .AddPolicy(FeedSourceAuth.Policy, policy => policy
                .AddAuthenticationSchemes(FeedSourceAuth.Scheme)
                .RequireClaim(FeedSourceAuth.SourceIdClaim));

        return services;
    }

    private static void Reject(HttpContext http, string message, string reason)
    {
        AuthMetrics.Failures.Add(1, new KeyValuePair<string, object?>("reason", reason));

        var logger = http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("SmartRail.Auth");
        logger.LogWarning("{Message}: reason={Reason} subject={Subject} {Method} {Path}",
            message, reason, Clean(http.User.FindFirst("sub")?.Value) ?? "-", Clean(http.Request.Method), Clean(http.Request.Path.Value));
    }

    // Values that came from a request are stripped of line breaks before logging (log forging).
    private static string? Clean(string? value) =>
        value is null
            ? null
            : new string(
                value
                    .Replace("\r", string.Empty)
                    .Replace("\n", string.Empty)
                    .Where(c => !char.IsControl(c))
                    .ToArray());

    private static Task WriteProblem(HttpContext http, int status, string title)
    {
        http.Response.StatusCode = status;
        return http.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title },
            options: null,
            contentType: "application/problem+json");
    }
}
