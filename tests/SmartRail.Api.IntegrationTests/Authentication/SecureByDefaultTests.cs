using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace SmartRail.Api.IntegrationTests.Authentication;

public sealed class SecureByDefaultTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // The only routes that may be reached without signing in. Anything else fails the build.
    // Passenger-facing public reads are added here deliberately, one by one, as they are built.
    private static readonly string[] AnonymousPrefixes =
    [
        "/health",
        "/alive",
        "/openapi",
        "/scalar",
        // Network reference data (SPEC-002): public, read-only.
        "/api/network/stations",
        "/api/network/routes"
    ];

    [Fact]
    public void Only_listed_routes_allow_anonymous_access()
    {
        _ = factory.CreateClient(); // starts the host

        var dataSources = factory.Services.GetServices<EndpointDataSource>();
        var anonymous = dataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(e => "/" + e.RoutePattern.RawText?.TrimStart('/'))
            .ToList();

        var unexpected = anonymous
            .Where(route => !AnonymousPrefixes.Any(prefix => route.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.True(unexpected.Count == 0, "Anonymous endpoints not on the allow-list: " + string.Join(", ", unexpected));
    }

    [Fact]
    public async Task Health_endpoint_is_reachable_without_a_token()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task An_unmapped_route_does_not_leak_data_anonymously()
    {
        var response = await factory.CreateClient().GetAsync("/api/does-not-exist");

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.NotFound });
    }

    [Fact]
    public async Task Api_document_offers_keycloak_sign_in()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("OAuth2", body);
        Assert.Contains("/protocol/openid-connect/auth", body);
    }
}
