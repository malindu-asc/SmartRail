using System.Net;

namespace SmartRail.Api.IntegrationTests.Authentication;

// Separation of duties: each role reaches only its own policy.
public sealed class RolePolicyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("driver", "Driver", HttpStatusCode.OK)]
    [InlineData("driver", "Controller", HttpStatusCode.Forbidden)]
    [InlineData("driver", "Admin", HttpStatusCode.Forbidden)]
    [InlineData("controller", "Controller", HttpStatusCode.OK)]
    [InlineData("controller", "Admin", HttpStatusCode.Forbidden)]
    [InlineData("controller", "Driver", HttpStatusCode.Forbidden)]
    [InlineData("admin", "Admin", HttpStatusCode.OK)]
    [InlineData("admin", "Controller", HttpStatusCode.Forbidden)]
    [InlineData("admin", "Driver", HttpStatusCode.Forbidden)]
    [InlineData("passenger", "Driver", HttpStatusCode.Forbidden)]
    [InlineData("passenger", "Controller", HttpStatusCode.Forbidden)]
    [InlineData("passenger", "Admin", HttpStatusCode.Forbidden)]
    public async Task Role_reaches_only_its_own_policy(string role, string policy, HttpStatusCode expected)
    {
        var client = factory.CreateAuthenticatedClient(factory.Tokens.Issue(roles: [role]));

        var response = await client.GetAsync($"/api/identity/policy-check/{policy}");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_policy_is_not_found()
    {
        var client = factory.CreateAuthenticatedClient(factory.Tokens.Issue(roles: ["admin"]));

        var response = await client.GetAsync("/api/identity/policy-check/Nonsense");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Policy_check_without_a_token_is_rejected()
    {
        var response = await factory.CreateClient().GetAsync("/api/identity/policy-check/Driver");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
