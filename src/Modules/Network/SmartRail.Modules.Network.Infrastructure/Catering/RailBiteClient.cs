using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartRail.Modules.Network.Application.Stations.GetStationMenu;

namespace SmartRail.Modules.Network.Infrastructure.Catering;

public sealed class CateringOptions
{
    public const string Section = "Catering";

    // For example http://localhost:4000. Empty means no catering provider is set up: every station then has no food row.
    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}

// The only place SmartRail talks to the RailBite catering service. It never retries and gives up quickly, so a slow kitchen
// cannot slow the railway down.
internal sealed class RailBiteClient(HttpClient http, IOptions<CateringOptions> options) : ICateringProvider
{
    public async Task<ProviderMenu?> GetMenuAsync(string stationCode, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
            return null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{settings.BaseUrl.TrimEnd('/')}/v1/stations/{Uri.EscapeDataString(stationCode)}/menu");
            request.Headers.Add("x-api-key", settings.ApiKey);

            using var response = await http.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            if (!response.IsSuccessStatusCode)
                throw new CateringUnavailableException($"RailBite answered {(int)response.StatusCode}.");

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            return new ProviderMenu(
                body.GetProperty("stationCode").GetString()!,
                body.GetProperty("currency").GetString()!,
                body.GetProperty("orderPhone").GetString()!,
                body.GetProperty("categories").EnumerateArray().Select(c => new ProviderCategory(
                    c.GetProperty("id").GetString()!,
                    Names(c.GetProperty("name")),
                    c.GetProperty("items").EnumerateArray().Select(i => new ProviderItem(
                        i.GetProperty("id").GetString()!, Names(i.GetProperty("name")), i.GetProperty("price").GetDecimal())).ToList())).ToList());
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new CateringUnavailableException("RailBite could not be reached or answered something unexpected.", ex);
        }
    }

    private static Dictionary<string, string> Names(JsonElement element) =>
        element.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty);
}
