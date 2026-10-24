using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SmartRail.Api.Core;
using SmartRail.Modules.Ticketing.Application.Tickets;
using SmartRail.Modules.Ticketing.Domain.Bookings;

namespace SmartRail.Modules.Ticketing.Api;

public static class TicketingEndpoints
{
    public static IEndpointRouteBuilder MapTicketingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ticketing").WithTags("Ticketing");

        // Browsing the offers needs no account; booking does (ASM-012).
        group.MapGet("/offers", async (HttpContext http, string from, string to, DateTimeOffset? after, int? adults, int? children, int? seniors, string? lang, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetOffersQuery(from, to, after, adults ?? 1, children ?? 0, seniors ?? 0, Language(http, lang)), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
            })
            .AllowAnonymous()
            .WithSummary("Trains with their fare and whether a seat is needed");

        group.MapGet("/journeys/{journeyId:guid}/seats", async (ClaimsPrincipal user, Guid journeyId, string from, string to, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSeatMapQuery(journeyId, from, to, UserId(user)), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
            })
            .RequireAuthorization()
            .WithSummary("The seat map of a train between two stops");

        group.MapPost("/seat-holds", async (ClaimsPrincipal user, SeatHoldRequest request, ISender sender, CancellationToken ct) =>
            {
                var seats = (request.Seats ?? []).Select(s => new SeatRef((s.Coach ?? string.Empty).ToUpperInvariant(), s.Number)).ToList();
                var result = await sender.Send(new HoldSeatsCommand(UserId(user)!, request.JourneyId, request.From ?? string.Empty, request.To ?? string.Empty, seats), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
            })
            .RequireAuthorization()
            .WithSummary("Hold seats for a few minutes while the booking is finished");

        group.MapPost("/bookings", async (HttpContext http, ClaimsPrincipal user, BookingRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CreateBookingCommand(
                    UserId(user)!, Email(user), request.JourneyId, request.From ?? string.Empty, request.To ?? string.Empty,
                    request.Adults, request.Children, request.Seniors, request.NeedsHelp, request.HoldId, Language(http, null)), ct);
                return result.IsSuccess ? Results.Created($"/api/ticketing/tickets/{result.Value.Summary.Id}", result.Value) : ApiResults.Problem(result);
            })
            .RequireAuthorization()
            .WithSummary("Confirm a booking (no payment is taken in this version)");

        group.MapGet("/tickets", async (HttpContext http, ClaimsPrincipal user, string? scope, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ListTicketsQuery(UserId(user)!, string.Equals(scope, "past", StringComparison.OrdinalIgnoreCase), Language(http, null)), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
            })
            .RequireAuthorization()
            .WithSummary("My tickets, active or past");

        group.MapGet("/tickets/{id:guid}", async (HttpContext http, ClaimsPrincipal user, Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetTicketQuery(UserId(user)!, id, Language(http, null)), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
            })
            .RequireAuthorization()
            .WithSummary("One ticket with its signed QR code");

        group.MapPost("/tickets/claim", async (HttpContext http, ClaimsPrincipal user, ClaimRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ClaimTicketCommand(UserId(user)!, Email(user), request.BookingCode ?? string.Empty, request.Contact ?? string.Empty, Language(http, null)), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
            })
            .RequireAuthorization()
            .WithSummary("Add a ticket to my account from its booking number");

        return app;
    }

    private static string? UserId(ClaimsPrincipal user) => user.FindFirstValue("sub");

    private static string? Email(ClaimsPrincipal user) => user.FindFirstValue("email");

    // ?lang= wins; otherwise the first language in Accept-Language.
    private static string? Language(HttpContext http, string? lang)
    {
        if (!string.IsNullOrWhiteSpace(lang))
            return lang;

        var header = http.Request.Headers.AcceptLanguage.ToString();
        var first = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return first?.Split(';')[0].Split('-')[0];
    }
}

public sealed record SeatRequest(string? Coach, int Number);

public sealed record SeatHoldRequest(Guid JourneyId, string? From, string? To, IReadOnlyList<SeatRequest>? Seats);

public sealed record BookingRequest(Guid JourneyId, string? From, string? To, int Adults, int Children, int Seniors, bool NeedsHelp, Guid? HoldId);

public sealed record ClaimRequest(string? BookingCode, string? Contact);
