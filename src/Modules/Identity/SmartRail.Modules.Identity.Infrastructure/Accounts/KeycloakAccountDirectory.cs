using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartRail.Modules.Identity.Application.Abstractions;
using SmartRail.Modules.Identity.Domain.Errors;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Identity.Infrastructure.Accounts;

public sealed class KeycloakAdminOptions
{
    public const string Section = "Identity:Admin";

    // For example http://localhost:8080 (no trailing slash). When empty it is taken from Authentication:Authority.
    public string BaseUrl { get; set; } = string.Empty;

    public string Realm { get; set; } = "smartrail";

    public string ClientId { get; set; } = "smartrail-identity-admin";

    public string ClientSecret { get; set; } = string.Empty;
}

// Keycloak's admin API, called with the identity-admin service account (it can only manage users).
internal sealed class KeycloakAccountDirectory(HttpClient http, IOptions<KeycloakAdminOptions> options, TimeProvider clock) : IAccountDirectory
{
    private readonly KeycloakAdminOptions _options = options.Value;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _token;
    private DateTimeOffset _tokenExpires;

    private string Users => $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users";

    public async Task<Result<string>> CreateAsync(string fullName, string email, string password, CancellationToken cancellationToken)
    {
        try
        {
            var (first, last) = SplitName(fullName);
            using var request = await AuthorizedAsync(HttpMethod.Post, Users, cancellationToken);
            request.Content = JsonContent.Create(new
            {
                username = email,
                email,
                firstName = first,
                lastName = last,
                enabled = true,
                // There is no email service in the MVP, so the address is taken as given.
                emailVerified = true,
                credentials = new[] { new { type = "password", value = password, temporary = false } }
            });

            using var response = await http.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Conflict)
                return IdentityErrors.AccountExists;
            if (!response.IsSuccessStatusCode)
                return IdentityErrors.DirectoryUnavailable;

            var id = response.Headers.Location?.Segments.LastOrDefault()?.Trim('/');
            if (string.IsNullOrEmpty(id))
                return IdentityErrors.DirectoryUnavailable;

            await GrantPassengerRoleAsync(id, cancellationToken);
            return id;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return IdentityErrors.DirectoryUnavailable;
        }
    }

    public async Task<AccountProfile?> FindAsync(string accountId, CancellationToken cancellationToken)
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Get, $"{Users}/{Uri.EscapeDataString(accountId)}", cancellationToken);
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var user = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            var first = user.TryGetProperty("firstName", out var f) ? f.GetString() : null;
            var last = user.TryGetProperty("lastName", out var l) ? l.GetString() : null;
            var email = user.TryGetProperty("email", out var e) ? e.GetString() ?? string.Empty : string.Empty;

            return new AccountProfile(accountId, string.Join(' ', new[] { first, last == NoLastName ? null : last }.Where(s => !string.IsNullOrWhiteSpace(s))), email);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    public async Task<Result> DeleteAsync(string accountId, CancellationToken cancellationToken)
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Delete, $"{Users}/{Uri.EscapeDataString(accountId)}", cancellationToken);
            using var response = await http.SendAsync(request, cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.NoContent or HttpStatusCode.OK => Result.Success(),
                HttpStatusCode.NotFound => IdentityErrors.AccountNotFound,
                _ => IdentityErrors.DirectoryUnavailable
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return IdentityErrors.DirectoryUnavailable;
        }
    }

    public async Task SendPasswordResetAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            using var find = await AuthorizedAsync(HttpMethod.Get, $"{Users}?email={Uri.EscapeDataString(email)}&exact=true", cancellationToken);
            using var found = await http.SendAsync(find, cancellationToken);
            if (!found.IsSuccessStatusCode)
                return;

            var users = await found.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            if (users.ValueKind != JsonValueKind.Array || users.GetArrayLength() == 0)
                return;

            var id = users[0].GetProperty("id").GetString();
            using var send = await AuthorizedAsync(HttpMethod.Put, $"{Users}/{id}/execute-actions-email", cancellationToken);
            send.Content = JsonContent.Create(new[] { "UPDATE_PASSWORD" });
            using var _ = await http.SendAsync(send, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // Silent on purpose: the caller must not learn anything from a failure either.
        }
    }

    private async Task GrantPassengerRoleAsync(string accountId, CancellationToken cancellationToken)
    {
        var rolesUrl = $"{_options.BaseUrl}/admin/realms/{_options.Realm}/roles/passenger";
        using var get = await AuthorizedAsync(HttpMethod.Get, rolesUrl, cancellationToken);
        using var role = await http.SendAsync(get, cancellationToken);
        if (!role.IsSuccessStatusCode)
            return;

        var body = await role.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        using var map = await AuthorizedAsync(HttpMethod.Post, $"{Users}/{accountId}/role-mappings/realm", cancellationToken);
        map.Content = JsonContent.Create(new[] { new { id = body.GetProperty("id").GetString(), name = "passenger" } });
        using var _ = await http.SendAsync(map, cancellationToken);
    }

    private async Task<HttpRequestMessage> AuthorizedAsync(HttpMethod method, string url, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(cancellationToken));
        return request;
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

    // Stored when the person gave a single name. It is never shown.
    private const string NoLastName = "-";

    private static (string First, string Last) SplitName(string fullName)
    {
        var name = fullName.Trim();
        var space = name.LastIndexOf(' ');

        // Keycloak will not finish setting up an account without a last name, and a person may have one name only.
        return space < 0 ? (name, NoLastName) : (name[..space].Trim(), name[(space + 1)..].Trim());
    }
}
