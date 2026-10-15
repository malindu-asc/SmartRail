using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace SmartRail.Api.IntegrationTests.Authentication;

public sealed class TokenValidationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string WhoAmI = "/api/identity/whoami";

    private sealed record WhoAmIBody(string? Subject, string? UserName, string[] Roles);

    [Fact]
    public async Task No_token_is_rejected_with_401()
    {
        var response = await factory.CreateClient().GetAsync(WhoAmI);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Valid_token_returns_subject_and_roles()
    {
        var token = factory.Tokens.Issue(roles: ["driver"]);

        var response = await factory.CreateAuthenticatedClient(token).GetAsync(WhoAmI);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<WhoAmIBody>();
        Assert.Equal("d0d0d0d0-0000-4000-8000-000000000001", body!.Subject);
        Assert.Contains("driver", body.Roles);
    }

    [Fact]
    public async Task Expired_token_is_rejected()
    {
        var token = factory.Tokens.Issue(roles: ["driver"], expires: DateTime.UtcNow.AddMinutes(-10));

        var response = await factory.CreateAuthenticatedClient(token).GetAsync(WhoAmI);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_from_another_issuer_is_rejected()
    {
        var token = factory.Tokens.Issue(roles: ["driver"], issuer: "https://evil.example/realms/smartrail");

        var response = await factory.CreateAuthenticatedClient(token).GetAsync(WhoAmI);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_for_another_audience_is_rejected()
    {
        var token = factory.Tokens.Issue(roles: ["driver"], audience: "some-other-api");

        var response = await factory.CreateAuthenticatedClient(token).GetAsync(WhoAmI);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_signed_with_an_unknown_key_is_rejected()
    {
        var foreignKey = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "foreign" };
        var token = factory.Tokens.Issue(roles: ["controller"], key: foreignKey);

        var response = await factory.CreateAuthenticatedClient(token).GetAsync(WhoAmI);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
