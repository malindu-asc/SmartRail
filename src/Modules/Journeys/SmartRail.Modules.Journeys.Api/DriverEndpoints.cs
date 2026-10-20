using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SmartRail.Api.Core;
using SmartRail.Api.Core.Security;
using SmartRail.Modules.Journeys.Application.Driver;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Api;

// The driver app (SPEC-011). A driver reaches only their own duty and the journeys on it: someone else's journey answers 404.
// Messages from control, acknowledging them and asking for assistance are in the Operations module under the same /api/driver prefix.
public static class DriverEndpoints
{
    public static IEndpointRouteBuilder MapDriverEndpoints(this IEndpointRouteBuilder app)
    {
        var driver = app.MapGroup("/api/driver").WithTags("Driver app");

        driver.MapGet("/duty", async (ISender sender, CancellationToken ct) => Respond(await sender.Send(new GetMyDutyQuery(), ct)))
            .RequirePermission(Permissions.DutyOwn)
            .WithSummary("My duty today: the shifts, each with its journeys (Next, Later, Running, Done) and whether I can sign on now");

        driver.MapGet("/duty/history", async (ISender sender, CancellationToken ct) => Respond(await sender.Send(new GetDutyHistoryQuery(), ct)))
            .RequirePermission(Permissions.DutyOwn)
            .WithSummary("My past duties from the last two weeks");

        driver.MapPost("/duty/{id:guid}/sign-on", async (Guid id, ISender sender, CancellationToken ct) => NoContent(await sender.Send(new SignOnDutyCommand(id), ct)))
            .RequirePermission(Permissions.DutyOwn)
            .WithSummary("Sign on to my duty (from 30 minutes before it starts)");

        driver.MapPost("/duty/{id:guid}/sign-off", async (Guid id, ISender sender, CancellationToken ct) => NoContent(await sender.Send(new SignOffDutyCommand(id), ct)))
            .RequirePermission(Permissions.DutyOwn)
            .WithSummary("Sign off my duty");

        driver.MapGet("/journeys/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => Respond(await sender.Send(new GetDriverJourneyQuery(id), ct)))
            .RequirePermission(Permissions.DutyOwn)
            .WithSummary("One journey on my duty: its stops, the next one-tap event, delay and whether I can act");

        driver.MapPost("/journeys/{id:guid}/events", async (Guid id, DriverEventRequest r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new RecordDriverEventCommand(id, r.StopSequence, r.Type ?? string.Empty, r.Sequence, r.OccurredAtUtc), ct)))
            .RequirePermission(Permissions.JourneyStopEventRecord)
            .WithSummary("Arrived, departed or skipped at a stop. Send the app's own rising sequence number so a retry or an offline replay is stored once");

        driver.MapPost("/journeys/{id:guid}/delay-reason", async (Guid id, DelayReasonRequest r, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new RecordDelayReasonCommand(id, r.ReasonCode ?? string.Empty), ct)))
            .RequirePermission(Permissions.JourneyDelayReasonRecord)
            .WithSummary("Why the train is late: signal_fault, door_fault, crew, passenger_incident, track or other");

        return app;
    }

    private static IResult Respond<T>(Result<T> result) => result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    private static IResult NoContent(Result result) => result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result);
}

public sealed record DriverEventRequest(int StopSequence, string? Type, long Sequence, DateTimeOffset? OccurredAtUtc);

public sealed record DelayReasonRequest(string? ReasonCode);
