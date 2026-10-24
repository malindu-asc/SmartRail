using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SmartRail.Api.Core;
using SmartRail.Modules.Network.Application.Routes.GetRoute;
using SmartRail.Modules.Network.Application.Routes.GetRoutes;
using SmartRail.Modules.Network.Application.Stations.GetStation;
using SmartRail.Modules.Network.Application.Stations.GetStationMenu;
using SmartRail.Modules.Network.Application.Stations.GetStations;
using SmartRail.Modules.Network.Application.Timetables.GetService;
using SmartRail.Modules.Network.Application.Timetables.GetStationTimetable;

namespace SmartRail.Modules.Network.Api;

// Public, read-only. The API requires authentication by default, so each endpoint opts out explicitly.
public static class NetworkEndpoints
{
    public static IEndpointRouteBuilder MapNetworkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/network").WithTags(Tags.Network).AllowAnonymous();

        group.MapGet("/stations", async (HttpContext http, string? q, string? lang, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetStationsQuery(q, RequestedLanguage(http, lang)), ct);
                return result.IsSuccess ? PublicCaching.Ok(http, result.Value) : ApiResults.Problem(result);
            })
            .WithSummary("List stations, optionally filtered by name or code");

        group.MapGet("/stations/{code}/menu", async (HttpContext http, string code, string? lang, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetStationMenuQuery(code, RequestedLanguage(http, lang)), ct);
                return result.IsSuccess ? PublicCaching.Ok(http, result.Value, 60) : ApiResults.Problem(result);
            })
            .WithSummary("The food menu at a station, from the catering provider");

        group.MapGet("/stations/{code}", async (HttpContext http, string code, string? lang, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetStationQuery(code, RequestedLanguage(http, lang)), ct);
                return result.IsSuccess ? PublicCaching.Ok(http, result.Value) : ApiResults.Problem(result);
            })
            .WithSummary("A station with its platforms");

        group.MapGet("/routes", async (HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetRoutesQuery(), ct);
                return result.IsSuccess ? PublicCaching.Ok(http, result.Value) : ApiResults.Problem(result);
            })
            .WithSummary("List routes");

        group.MapGet("/routes/{code}", async (HttpContext http, string code, string? lang, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetRouteQuery(code, RequestedLanguage(http, lang)), ct);
                return result.IsSuccess ? PublicCaching.Ok(http, result.Value) : ApiResults.Problem(result);
            })
            .WithSummary("A route with its public stops");

        group.MapGet("/stations/{code}/timetable", async (HttpContext http, string code, string? date, string? kind, string? lang, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetStationTimetableQuery(code, date, kind, RequestedLanguage(http, lang)), ct);
                return result.IsSuccess ? PublicCaching.Ok(http, result.Value) : ApiResults.Problem(result);
            })
            .WithSummary("Scheduled departures or arrivals at a station on a date");

        group.MapGet("/services/{serviceId:guid}", async (HttpContext http, Guid serviceId, string? date, string? lang, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetServiceQuery(serviceId, date, RequestedLanguage(http, lang)), ct);
                return result.IsSuccess ? PublicCaching.Ok(http, result.Value) : ApiResults.Problem(result);
            })
            .WithSummary("The public stops and scheduled times of a service on a date");

        return app;
    }

    // ?lang= wins; otherwise the first language in Accept-Language (for example "si-LK,en;q=0.8" gives "si").
    private static string? RequestedLanguage(HttpContext http, string? lang)
    {
        if (!string.IsNullOrWhiteSpace(lang))
            return lang;

        var header = http.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(header))
            return null;

        var first = header.Split(',')[0].Split(';')[0].Trim();
        var dash = first.IndexOf('-');
        return dash > 0 ? first[..dash] : first;
    }
}
