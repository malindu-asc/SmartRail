using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Api.Core.Security;

namespace SmartRail.Api.IntegrationTests.Authentication;

// The controller, scheduler and administrator consoles: every endpoint asks for one known permission, and a role without it is turned
// away before any handler runs (so these tests need no database).
public sealed class ConsoleEndpointPolicyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly string[] ProtectedPrefixes = ["/api/ops", "/api/admin", "/api/driver", "/api/audit"];

    private IEnumerable<RouteEndpoint> ConsoleEndpoints() =>
        factory.Services.GetServices<EndpointDataSource>().SelectMany(s => s.Endpoints).OfType<RouteEndpoint>()
            .Where(e => ProtectedPrefixes.Any(p => e.RoutePattern.RawText!.StartsWith(p, StringComparison.OrdinalIgnoreCase)));

    [Fact]
    public void Every_console_endpoint_requires_sign_in_and_names_a_known_permission()
    {
        var endpoints = ConsoleEndpoints().ToList();
        Assert.NotEmpty(endpoints);

        foreach (var endpoint in endpoints)
        {
            var authorize = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            Assert.True(authorize.Count > 0, $"{endpoint.RoutePattern.RawText} is open to anyone");
            Assert.DoesNotContain(endpoint.Metadata, m => m is IAllowAnonymous);

            foreach (var policy in authorize.Select(a => a.Policy).Where(p => !string.IsNullOrEmpty(p)))
                Assert.Contains(policy!, Permissions.All);
        }
    }

    [Fact]
    public void Each_permission_used_by_a_console_endpoint_is_held_by_some_role()
    {
        var used = ConsoleEndpoints().SelectMany(e => e.Metadata.GetOrderedMetadata<IAuthorizeData>())
            .Select(a => a.Policy).Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();

        foreach (var permission in used)
            Assert.True(RolePermissions.Matrix.Values.Any(p => p.Contains(permission!)), $"No role holds {permission}");
    }

    // A role without the permission gets 403 on every endpoint that asks for it; no token gets 401.
    [Fact]
    public async Task A_role_without_the_permission_is_forbidden_and_an_anonymous_caller_is_unauthorised()
    {
        var anonymous = factory.CreateClient();
        var clients = Roles.All.ToDictionary(r => r, r => factory.CreateAuthenticatedClient(factory.Tokens.Issue(roles: [r])));

        foreach (var endpoint in ConsoleEndpoints())
        {
            var permission = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(a => a.Policy).FirstOrDefault(p => !string.IsNullOrEmpty(p));
            var path = endpoint.RoutePattern.RawText!.Replace("{id:guid}", Guid.NewGuid().ToString()).Replace("{serviceId:guid}", Guid.NewGuid().ToString())
                .Replace("{periodId:guid}", Guid.NewGuid().ToString()).Replace("{key}", "KEY").Replace("{id}", "x");
            if (path.Contains('{'))
                continue;

            foreach (var method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods)
            {
                var anonymousResponse = await anonymous.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
                Assert.True(anonymousResponse.StatusCode == HttpStatusCode.Unauthorized, $"{method} {path} anonymous gave {anonymousResponse.StatusCode}");

                if (permission is null)
                    continue;

                foreach (var (role, client) in clients.Where(c => !RolePermissions.Allows([c.Key], permission)))
                {
                    var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) });
                    Assert.True(response.StatusCode == HttpStatusCode.Forbidden, $"{method} {path} as {role} gave {response.StatusCode}");
                }
            }
        }
    }
}
