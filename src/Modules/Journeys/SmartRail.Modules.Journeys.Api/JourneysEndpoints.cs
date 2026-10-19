using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using SmartRail.Api.Core;
using SmartRail.Api.Core.Security;
using SmartRail.Modules.Journeys.Application.Ingest;
using SmartRail.Modules.Journeys.Application.Manual;
using SmartRail.Modules.Journeys.Application.Queries.GetBoard;
using SmartRail.Modules.Journeys.Application.Queries.GetJourney;

namespace SmartRail.Modules.Journeys.Api;

public static class JourneysEndpoints
{
    // Live data changes every few seconds, so public reads are cached for 5 seconds only.
    private const int LiveMaxAgeSeconds = 5;

    public static IEndpointRouteBuilder MapJourneysEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/journeys").WithTags(Tags.Journeys);

        group.MapGet("/{journeyId:guid}", async (HttpContext http, Guid journeyId, string? lang, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetJourneyQuery(journeyId, RequestedLanguage(http, lang)), ct);
                return result.IsSuccess ? PublicCaching.Ok(http, result.Value, LiveMaxAgeSeconds) : ApiResults.Problem(result);
            })
            .AllowAnonymous()
            .WithSummary("A journey with its stops and scheduled times");

        group.MapGet("/boards/{stationCode}", async (HttpContext http, string stationCode, int? count, string? lang, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetBoardQuery(stationCode, count ?? 10, RequestedLanguage(http, lang)), ct);
                return result.IsSuccess ? PublicCaching.Ok(http, result.Value, LiveMaxAgeSeconds) : ApiResults.Problem(result);
            })
            .AllowAnonymous()
            .WithSummary("The next departures from a station");

        group.MapPost("/manual", async (CreateManualJourneyRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new CreateManualJourneyCommand(request.RouteCode, request.TrainNumber, request.ServiceDate, request.Stops, request.Reason), ct);

                return result.IsSuccess
                    ? Results.Created($"/api/journeys/{result.Value}", new { journeyId = result.Value })
                    : ApiResults.Problem(result);
            })
            .RequireAuthorization(Policies.Scheduler)
            .WithSummary("Create a special or relief journey that is not in the timetable");

        // The train detection feed (and the simulator) send live data here with their own source key, not a user token.
        var ingest = app.MapGroup("/api/ingest").WithTags(Tags.Ingest).RequireAuthorization(FeedSourceAuth.Policy);

        ingest.MapPost("/stop-events", async (IngestStopEventsRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            {
                var events = (request.Events ?? []).Select(e => new StopEventInput(e.Sequence, e.JourneyId, e.StopSequence, e.Type ?? string.Empty, e.OccurredAt)).ToList();
                var result = await sender.Send(new IngestStopEventsCommand(Sender(user), events), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
            })
            .WithSummary("Send stop events (arrived, departed, passed) from the feed");

        ingest.MapPost("/positions", async (IngestPositionsRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            {
                var readings = (request.Readings ?? []).Select(r => new PositionInput(
                    r.Sequence, r.VehicleCode ?? string.Empty, r.RecordedAt, r.FromStation ?? string.Empty, r.ToStation ?? string.Empty, r.Progress, r.SpeedKmh)).ToList();
                var result = await sender.Send(new IngestPositionsCommand(Sender(user), readings), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
            })
            .WithSummary("Send vehicle positions from the feed");

        return app;
    }

    private static FeedSender Sender(ClaimsPrincipal user) => new(
        Guid.Parse(user.FindFirstValue(FeedSourceAuth.SourceIdClaim)!),
        user.FindFirstValue(FeedSourceAuth.SourceCodeClaim)!,
        int.Parse(user.FindFirstValue(FeedSourceAuth.SourceRankClaim)!));

    // ?lang= wins; otherwise the first language in Accept-Language.
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

public sealed record CreateManualJourneyRequest(
    string RouteCode,
    string TrainNumber,
    DateOnly ServiceDate,
    IReadOnlyList<ManualStop> Stops,
    string Reason);

public sealed record IngestStopEventsRequest(IReadOnlyList<StopEventRequest>? Events);

public sealed record StopEventRequest(long Sequence, Guid JourneyId, int StopSequence, string? Type, DateTimeOffset OccurredAt);

public sealed record IngestPositionsRequest(IReadOnlyList<PositionRequest>? Readings);

public sealed record PositionRequest(
    long Sequence,
    string? VehicleCode,
    DateTimeOffset RecordedAt,
    string? FromStation,
    string? ToStation,
    decimal Progress,
    decimal SpeedKmh);
