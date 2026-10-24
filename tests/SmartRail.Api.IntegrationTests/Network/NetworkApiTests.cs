using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartRail.Api.IntegrationTests.Network;

public sealed class NetworkApiTests(NetworkApiFactory factory) : IClassFixture<NetworkApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<JsonElement> GetJsonAsync(string url, Action<HttpRequestMessage>? configure = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        configure?.Invoke(request);
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    // AC-1
    [Fact]
    public async Task Lists_all_twelve_stations_sorted_by_name()
    {
        var json = await GetJsonAsync("/api/network/stations");

        var items = json.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(12, items.Count);
        var names = items.Select(i => i.GetProperty("name").GetString()!).ToList();
        Assert.Equal(names.Order(StringComparer.OrdinalIgnoreCase).ToList(), names);
        Assert.All(items, i => Assert.False(string.IsNullOrEmpty(i.GetProperty("code").GetString())));
        Assert.Equal("en", json.GetProperty("language").GetString());
    }

    // AC-2
    [Fact]
    public async Task Search_filters_by_name_or_code_and_rejects_one_character()
    {
        var json = await GetJsonAsync("/api/network/stations?q=OAK");
        var codes = json.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("code").GetString()).ToList();
        Assert.Equal(["OAK"], codes);

        var response = await _client.GetAsync("/api/network/stations?q=o");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // AC-3
    [Fact]
    public async Task Station_is_found_by_code_in_any_case_with_platforms()
    {
        var json = await GetJsonAsync("/api/network/stations/cen");
        Assert.Equal("CEN", json.GetProperty("code").GetString());
        Assert.Equal(3, json.GetProperty("platforms").GetArrayLength());

        var missing = await _client.GetAsync("/api/network/stations/ZZZ");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    // AC-4
    [Fact]
    public async Task Names_follow_the_requested_language_with_fallback()
    {
        var byQuery = await GetJsonAsync("/api/network/stations/CEN?lang=si");
        Assert.Equal("si", byQuery.GetProperty("language").GetString());
        Assert.Equal("මධ්‍යම", byQuery.GetProperty("name").GetString());

        var byHeader = await GetJsonAsync("/api/network/stations/CEN", r => r.Headers.AcceptLanguage.ParseAdd("si-LK,en;q=0.8"));
        Assert.Equal("si", byHeader.GetProperty("language").GetString());

        var unknown = await GetJsonAsync("/api/network/stations/CEN?lang=xx");
        Assert.Equal("en", unknown.GetProperty("language").GetString());
        Assert.Equal("Central", unknown.GetProperty("name").GetString());
    }

    // AC-5
    [Fact]
    public async Task Route_comes_back_in_sequence_with_times_and_kinds()
    {
        var json = await GetJsonAsync("/api/network/routes/hill");
        var stops = json.GetProperty("stops").EnumerateArray().ToList();

        Assert.Equal(["CEN", "OAK", "HIL", "RIV", "MAR"], stops.Select(s => s.GetProperty("stationCode").GetString()));
        Assert.Equal([1, 2, 3, 4, 5], stops.Select(s => s.GetProperty("sequence").GetInt32()));
        Assert.Equal(0, stops[0].GetProperty("minutesFromPrevious").GetInt32());
        Assert.Equal("Stop", stops[0].GetProperty("kind").GetString());

        var missing = await _client.GetAsync("/api/network/routes/NOPE");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    // AC-6
    [Fact]
    public async Task Passing_points_are_not_in_the_public_route()
    {
        var json = await GetJsonAsync("/api/network/routes/COAST");
        var stops = json.GetProperty("stops").EnumerateArray().ToList();

        Assert.Equal(["CEN", "LAK", "BAY"], stops.Select(s => s.GetProperty("stationCode").GetString()));
        Assert.Equal([1, 2, 3], stops.Select(s => s.GetProperty("sequence").GetInt32()));

        var list = await GetJsonAsync("/api/network/routes");
        var coast = list.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("code").GetString() == "COAST");
        Assert.Equal(3, coast.GetProperty("stopCount").GetInt32());
    }

    // AC-9
    [Fact]
    public async Task Network_is_anonymous_but_everything_else_still_needs_a_token()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/network/stations")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/identity/whoami")).StatusCode);
    }

    [Fact]
    public async Task Public_reads_are_cacheable_and_revalidated_with_an_etag()
    {
        var first = await _client.GetAsync("/api/network/routes");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Contains("max-age=300", first.Headers.CacheControl!.ToString());
        var etag = first.Headers.ETag!.Tag;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/network/routes");
        request.Headers.IfNoneMatch.ParseAdd(etag);
        var second = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
    }
}
