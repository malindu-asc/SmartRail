using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace SmartRail.Modules.Identity.Infrastructure.Accounts;

// A small client for Keycloak's admin API, signed in as the identity-admin service account. Paths are relative to
// /admin/realms/{realm}. Used for staff accounts, sign-in events and the account-lock settings.
internal sealed class KeycloakAdminClient(HttpClient http, IOptions<KeycloakAdminOptions> options, TimeProvider clock)
{
    private readonly KeycloakAdminOptions _options = options.Value;
    // Shared by every instance: the client is created per request, the token should not be.
    private static readonly SemaphoreSlim _tokenLock = new(1, 1);
    private static string? _token;
    private static DateTimeOffset _tokenExpires;

    public string Realm => _options.Realm;

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, $"{_options.BaseUrl}/admin/realms/{_options.Realm}{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(cancellationToken));
        if (body is not null)
            request.Content = body is JsonNode node ? new StringContent(node.ToJsonString(), System.Text.Encoding.UTF8, "application/json") : JsonContent.Create(body);

        return await http.SendAsync(request, cancellationToken);
    }

    // The body as a JSON tree, or null when the answer was not a success.
    public async Task<JsonNode?> GetAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, path, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private async Task<string> TokenAsync(CancellationToken cancellationToken)
    {
        if (_token is not null && clock.GetUtcNow() < _tokenExpires)
            return _token;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_token is not null && clock.GetUtcNow() < _tokenExpires)
                return _token;

            using var response = await http.PostAsync(
                $"{_options.BaseUrl}/realms/{_options.Realm}/protocol/openid-connect/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret
                }),
                cancellationToken);

            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            _token = body.GetProperty("access_token").GetString()!;
            _tokenExpires = clock.GetUtcNow().AddSeconds(Math.Max(body.GetProperty("expires_in").GetInt32() - 30, 5));
            return _token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}
