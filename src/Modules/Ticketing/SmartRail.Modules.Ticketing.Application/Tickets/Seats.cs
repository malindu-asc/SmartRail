using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Contracts;
using SmartRail.Modules.Ticketing.Application.Abstractions;
using SmartRail.Modules.Ticketing.Domain.Bookings;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Ticketing.Application.Tickets;

// ---- The seat map ----

public sealed record GetSeatMapQuery(Guid JourneyId, string From, string To, string? UserId) : IQuery<SeatMapResponse>;

public sealed record SeatMapResponse(IReadOnlyList<string> Coaches, int SeatsPerCoach, IReadOnlyList<SeatRef> Taken, IReadOnlyList<SeatRef> Yours);

public sealed class GetSeatMapQueryHandler(IJourneyLookup journeys, IBookingStore store, TimeProvider clock)
    : IQueryHandler<GetSeatMapQuery, SeatMapResponse>
{
    public async Task<Result<SeatMapResponse>> Handle(GetSeatMapQuery query, CancellationToken cancellationToken)
    {
        var journey = await journeys.FindAsync(query.JourneyId, cancellationToken);
        var (from, to) = Segment.Of(journey, query.From, query.To);
        if (journey is null || from < 0)
            return TicketingErrors.JourneyNotFound(query.JourneyId);

        var (taken, yours) = await SeatUse.ReadAsync(store, query.JourneyId, from, to, query.UserId, clock.GetUtcNow(), cancellationToken);
        return new SeatMapResponse(SeatLayout.Coaches, SeatLayout.SeatsPerCoach, taken, yours);
    }
}

// ---- Hold seats while the passenger finishes ----

public sealed record HoldSeatsCommand(string UserId, Guid JourneyId, string From, string To, IReadOnlyList<SeatRef> Seats) : ICommand<SeatHoldResponse>;

public sealed record SeatHoldResponse(Guid HoldId, DateTimeOffset ExpiresAtUtc);

public sealed class HoldSeatsCommandHandler(
    IJourneyLookup journeys,
    IBookingStore store,
    ITicketingUnitOfWork unitOfWork,
    TicketingOptions options,
    TimeProvider clock)
    : ICommandHandler<HoldSeatsCommand, SeatHoldResponse>
{
    public async Task<Result<SeatHoldResponse>> Handle(HoldSeatsCommand command, CancellationToken cancellationToken)
    {
        var journey = await journeys.FindAsync(command.JourneyId, cancellationToken);
        var (from, to) = Segment.Of(journey, command.From, command.To);
        if (journey is null || from < 0)
            return TicketingErrors.JourneyNotFound(command.JourneyId);

        if (command.Seats.Count is < 1 or > 9 || command.Seats.Distinct().Count() != command.Seats.Count)
            return TicketingErrors.SeatCount;
        if (command.Seats.Any(s => !SeatLayout.Exists(s)))
            return TicketingErrors.SeatInvalid;

        var now = clock.GetUtcNow();
        await using var scope = await unitOfWork.LockJourneyAsync(command.JourneyId, cancellationToken);

        // Seats the person already holds count as free for them: choosing again replaces the earlier hold.
        var holds = await store.ListHoldsAsync(command.JourneyId, cancellationToken);
        foreach (var old in holds.Where(h => h.UserId == command.UserId))
            store.Remove(old);

        var (taken, _) = await SeatUse.ReadAsync(store, command.JourneyId, from, to, command.UserId, now, cancellationToken);
        if (command.Seats.Any(taken.Contains))
            return TicketingErrors.SeatTaken;

        var hold = new SeatHold(Guid.NewGuid(), command.UserId, command.JourneyId, from, to, command.Seats, now.AddMinutes(options.SeatHoldMinutes));
        store.Add(hold);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await scope.CommitAsync(cancellationToken);
        return new SeatHoldResponse(hold.Id, hold.ExpiresAtUtc);
    }
}

// Where a trip sits on a train, and which seats are in use on that part of the line.
internal static class Segment
{
    // The positions of the two stops in the journey's stop list, or (-1, -1) when the train does not run from one to the other.
    public static (int From, int To) Of(JourneyRef? journey, string from, string to)
    {
        if (journey is null)
            return (-1, -1);

        var codes = journey.StationCodes.ToList();
        var f = codes.IndexOf(from.Trim().ToUpperInvariant());
        var t = codes.LastIndexOf(to.Trim().ToUpperInvariant());
        return f >= 0 && t > f ? (f, t) : (-1, -1);
    }
}

internal static class SeatUse
{
    // Taken: seats in someone else's booking or hold on an overlapping part of the line. Yours: the caller's own active hold.
    public static async Task<(IReadOnlyList<SeatRef> Taken, IReadOnlyList<SeatRef> Yours)> ReadAsync(
        IBookingStore store, Guid journeyId, int from, int to, string? userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var taken = new HashSet<SeatRef>();
        var yours = new HashSet<SeatRef>();

        foreach (var booking in await store.ListForJourneyAsync(journeyId, cancellationToken))
        {
            if (SeatHold.Overlaps(from, to, booking.FromIndex, booking.ToIndex))
                taken.UnionWith(booking.Seats);
        }

        foreach (var hold in await store.ListHoldsAsync(journeyId, cancellationToken))
        {
            if (!hold.IsActive(now) || !SeatHold.Overlaps(from, to, hold.FromIndex, hold.ToIndex))
                continue;

            if (userId is not null && hold.UserId == userId)
                yours.UnionWith(hold.Seats);
            else
                taken.UnionWith(hold.Seats);
        }

        return (taken.ToList(), yours.ToList());
    }
}
