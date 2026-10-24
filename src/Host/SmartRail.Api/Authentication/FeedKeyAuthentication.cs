using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SmartRail.Api.Core.Security;
using SmartRail.Modules.Identity.Contracts;

namespace SmartRail.Api.Authentication;

// Machines that send live data authenticate with their own key, not a user token (REQ-SEC-004).
// The headers are X-Source-Code and X-Source-Key. Nothing here ever logs or returns a key.
internal sealed class FeedKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IFeedSources feedSources)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    private const int MaxCodeLength = 30;
    private const int MaxKeyLength = 200;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var code = Request.Headers[FeedSourceAuth.CodeHeader].ToString();
        var key = Request.Headers[FeedSourceAuth.KeyHeader].ToString();

        if (code.Length == 0 && key.Length == 0)
            return AuthenticateResult.NoResult();

        if (code.Length is 0 or > MaxCodeLength || key.Length is 0 or > MaxKeyLength)
            return AuthenticateResult.Fail("invalid_feed_credentials");

        var source = await feedSources.AuthenticateAsync(code, key, Context.RequestAborted);
        if (source is null)
            return AuthenticateResult.Fail("invalid_feed_credentials");

        var identity = new ClaimsIdentity(
            [
                new Claim(FeedSourceAuth.SourceIdClaim, source.Id.ToString()),
                new Claim(FeedSourceAuth.SourceCodeClaim, source.Code),
                new Claim(FeedSourceAuth.SourceRankClaim, source.Rank.ToString())
            ],
            Scheme.Name);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // The response never says whether the code or the key was wrong.
        var reason = Request.Headers.ContainsKey(FeedSourceAuth.KeyHeader) ? "feed_key_invalid" : "feed_key_missing";
        AuthMetrics.Failures.Add(1, new KeyValuePair<string, object?>("reason", reason));
        // Request values are stripped of line breaks before logging (log forging).
        var safeMethod = Request.Method.Replace("\r", string.Empty).Replace("\n", string.Empty);
        var safePath = (Request.Path.Value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
        Logger.LogWarning("Feed authentication refused: reason={Reason} {Method} {Path}", reason, safeMethod, safePath);

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        await Response.WriteAsJsonAsync(
            new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = "Unauthorized" },
            options: null,
            contentType: "application/problem+json");
    }
}
