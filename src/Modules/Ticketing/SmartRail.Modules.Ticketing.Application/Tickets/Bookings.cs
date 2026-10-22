using System.Security.Cryptography;
using FluentValidation;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Journeys.Contracts;
using SmartRail.Modules.Network.Contracts;
using SmartRail.Modules.Ticketing.Application.Abstractions;
using SmartRail.Modules.Ticketing.Domain.Bookings;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Ticketing.Application.Tickets;

// ---- What the passenger sees of a ticket ----

public sealed record TicketSummary(
    Guid Id,
    string Reference,
    string FromCode,
    string FromName,
    string ToCode,
    string ToName,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    string TimeZone,
    string State,
    int Passengers,
    IReadOnlyList<string> Seats);

public sealed record TicketDetail(
    TicketSummary Summary,
    string BookingCode,
    string TrainNumber,
    string JourneyId,
    int Adults,
    int Children,
    int Seniors,
    bool NeedsHelp,
    string Currency,
    decimal FareTotal,
    string QrPayload);

// Builds the passenger's view of a booking, with its state taken from the train as it is now.
public sealed class TicketViewer(IJourneyLookup journeys, INetworkStations stations, INetworkTimeZone timeZone, ITicketSigner signer, TimeProvider clock)
{
    public async Task<TicketSummary> SummaryAsync(Booking b, string? language, CancellationToken cancellationToken)
    {
        var journey = await journeys.FindAsync(b.JourneyId, cancellationToken);
        var names = await stations.GetNamesAsync([b.FromStationCode, b.ToStationCode], language, cancellationToken);
        var state = TicketStates.Evaluate(journey?.Lifecycle ?? "Planned", b.DepartureUtc, b.ArrivalUtc, clock.GetUtcNow());

        return new TicketSummary(
            b.Id, b.Reference, b.FromStationCode, names.GetValueOrDefault(b.FromStationCode, b.FromStationCode),
            b.ToStationCode, names.GetValueOrDefault(b.ToStationCode, b.ToStationCode),
            b.DepartureUtc, b.ArrivalUtc, timeZone.TimeZoneId, state.ToString(), b.Passengers,
            b.Seats.Select(s => s.ToString()).ToList());
    }

    public async Task<TicketDetail> DetailAsync(Booking b, string? language, CancellationToken cancellationToken) =>
        new(await SummaryAsync(b, language, cancellationToken), b.BookingCode, b.TrainNumber, b.JourneyId.ToString(),
            b.Adults, b.Children, b.Seniors, b.NeedsHelp, b.Currency, b.FareTotal, signer.Sign(b));
}

// ---- Confirm booking (no payment in the MVP) ----

public sealed record CreateBookingCommand(
    string UserId, string? Email, Guid JourneyId, string From, string To,
    int Adults, int Children, int Seniors, bool NeedsHelp, Guid? HoldId, string? Language) : ICommand<TicketDetail>;

public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.Adults).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Children).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Seniors).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Adults + x.Children + x.Seniors).InclusiveBetween(1, 9).WithMessage("Choose between 1 and 9 travellers.");
    }
}

public sealed class CreateBookingCommandHandler(
    IJourneySearch search,
    IJourneyLookup journeys,
    IBookingStore store,
    ITicketingUnitOfWork unitOfWork,
    TicketingOptions options,
    FareOptions fares,
    TicketViewer viewer,
    TimeProvider clock)
    : ICommandHandler<CreateBookingCommand, TicketDetail>
{
    public async Task<Result<TicketDetail>> Handle(CreateBookingCommand command, CancellationToken cancellationToken)
    {
        var from = command.From.Trim().ToUpperInvariant();
        var to = command.To.Trim().ToUpperInvariant();
        var now = clock.GetUtcNow();

        var offer = await search.GetOfferAsync(command.JourneyId, from, to, command.Language, cancellationToken);
        if (offer is null)
            return TicketingErrors.JourneyNotFound(command.JourneyId);

        // A train that has left, finished or been cancelled cannot be booked (the sales window).
        if (offer.Status is "Cancelled" or "Completed" or "Terminated" || offer.ExpectedDepartureUtc < now.AddMinutes(-1))
            return TicketingErrors.CannotBookThisTrain;

        var journey = await journeys.FindAsync(command.JourneyId, cancellationToken);
        var (fromIndex, toIndex) = Segment.Of(journey, from, to);
        if (journey is null || fromIndex < 0)
            return TicketingErrors.JourneyNotFound(command.JourneyId);

        var passengers = command.Adults + command.Children + command.Seniors;
        var seatRequired = options.SeatReservedRoutes.Contains(offer.RouteCode, StringComparer.OrdinalIgnoreCase);

        await using var scope = await unitOfWork.LockJourneyAsync(command.JourneyId, cancellationToken);

        IReadOnlyList<SeatRef> seats = [];
        if (seatRequired)
        {
            var hold = command.HoldId is { } id ? await store.GetHoldAsync(id, cancellationToken) : null;
            if (hold is null || hold.UserId != command.UserId || hold.JourneyId != command.JourneyId || !hold.IsActive(now))
                return TicketingErrors.HoldExpired;
            if (hold.Seats.Count != passengers)
                return TicketingErrors.SeatCount;

            // Someone else's booking can never have taken a held seat, but check again under the lock.
            var (taken, _) = await SeatUse.ReadAsync(store, command.JourneyId, fromIndex, toIndex, command.UserId, now, cancellationToken);
            if (hold.Seats.Any(taken.Contains))
                return TicketingErrors.SeatTaken;

            seats = hold.Seats.ToList();
            store.Remove(hold);
        }

        var minutes = (int)Math.Round((offer.ArrivalUtc - offer.DepartureUtc).TotalMinutes);
        var fare = FareCalculator.Quote(fares, minutes, command.Adults, command.Children, command.Seniors);

        var (reference, code) = await NewNumbersAsync(cancellationToken);
        var booking = new Booking(
            Guid.NewGuid(), reference, code, command.UserId, command.Email?.Trim().ToLowerInvariant(),
            command.JourneyId, offer.ServiceDate, offer.RouteCode, offer.TrainNumber, from, to, fromIndex, toIndex,
            offer.DepartureUtc, offer.ArrivalUtc, command.Adults, command.Children, command.Seniors, command.NeedsHelp,
            fare.Total, fare.Currency, seats, now);

        store.Add(booking);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await scope.CommitAsync(cancellationToken);

        return await viewer.DetailAsync(booking, command.Language, cancellationToken);
    }

    private async Task<(string Reference, string Code)> NewNumbersAsync(CancellationToken cancellationToken)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var reference = $"SR-{RandomNumberGenerator.GetInt32(10000):D4}-{RandomNumberGenerator.GetInt32(10000):D4}";
            var code = new string(Enumerable.Range(0, 8).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());
            if (!await store.ReferenceOrCodeExistsAsync(reference, code, cancellationToken))
                return (reference, code);
        }

        throw new InvalidOperationException("Could not find a free ticket number.");
    }
}

// ---- My tickets ----

public sealed record ListTicketsQuery(string UserId, bool Past, string? Language) : IQuery<IReadOnlyList<TicketSummary>>;

public sealed class ListTicketsQueryHandler(IBookingStore store, TicketViewer viewer)
    : IQueryHandler<ListTicketsQuery, IReadOnlyList<TicketSummary>>
{
    public async Task<Result<IReadOnlyList<TicketSummary>>> Handle(ListTicketsQuery query, CancellationToken cancellationToken)
    {
        var bookings = await store.ListForUserAsync(query.UserId, cancellationToken);
        var summaries = new List<TicketSummary>();
        foreach (var booking in bookings)
            summaries.Add(await viewer.SummaryAsync(booking, query.Language, cancellationToken));

        IReadOnlyList<TicketSummary> result = summaries
            .Where(s => (s.State == nameof(TicketState.Past)) == query.Past)
            .OrderBy(s => query.Past ? 0 : 1).ThenBy(s => query.Past ? -s.DepartureUtc.ToUnixTimeSeconds() : s.DepartureUtc.ToUnixTimeSeconds())
            .ToList();
        return Result.Success(result);
    }
}

public sealed record GetTicketQuery(string UserId, Guid TicketId, string? Language) : IQuery<TicketDetail>;

public sealed class GetTicketQueryHandler(IBookingStore store, TicketViewer viewer) : IQueryHandler<GetTicketQuery, TicketDetail>
{
    public async Task<Result<TicketDetail>> Handle(GetTicketQuery query, CancellationToken cancellationToken)
    {
        var booking = await store.GetAsync(query.TicketId, cancellationToken);

        // Someone else's ticket looks the same as no ticket.
        if (booking is null || booking.UserId != query.UserId)
            return TicketingErrors.BookingNotFound;

        return await viewer.DetailAsync(booking, query.Language, cancellationToken);
    }
}

// ---- Add a ticket from a booking number (another phone, a lost session) ----

public sealed record ClaimTicketCommand(string UserId, string? Email, string BookingCode, string Contact, string? Language) : ICommand<TicketDetail>;

public sealed class ClaimTicketCommandHandler(IBookingStore store, ITicketingUnitOfWork unitOfWork, TicketViewer viewer)
    : ICommandHandler<ClaimTicketCommand, TicketDetail>
{
    public async Task<Result<TicketDetail>> Handle(ClaimTicketCommand command, CancellationToken cancellationToken)
    {
        var code = command.BookingCode.Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
        var booking = await store.GetByCodeAsync(code, cancellationToken);

        // The booking number alone is not enough: the email used at purchase must match. Every mismatch gives the same answer.
        if (booking is null
            || string.IsNullOrWhiteSpace(booking.ContactEmail)
            || !string.Equals(booking.ContactEmail, command.Contact.Trim(), StringComparison.OrdinalIgnoreCase))
            return TicketingErrors.ClaimNotMatched;

        booking.AssignTo(command.UserId, command.Email?.Trim().ToLowerInvariant());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await viewer.DetailAsync(booking, command.Language, cancellationToken);
    }
}

// ---- Download and delete my data ----

public sealed class TicketsPersonalData(IBookingStore store, ITicketingUnitOfWork unitOfWork) : IPersonalDataContributor
{
    public string Name => "tickets";

    public async Task<object?> ExportAsync(string userId, CancellationToken cancellationToken)
    {
        var bookings = await store.ListForUserAsync(userId, cancellationToken);
        return bookings.Select(b => new
        {
            b.Reference, b.BookingCode, b.TrainNumber, b.ServiceDate, b.FromStationCode, b.ToStationCode,
            b.DepartureUtc, b.ArrivalUtc, b.Adults, b.Children, b.Seniors, b.NeedsHelp, b.FareTotal, b.Currency,
            Seats = b.Seats.Select(s => s.ToString()), b.ContactEmail
        }).ToList();
    }

    public async Task EraseAsync(string userId, CancellationToken cancellationToken)
    {
        foreach (var booking in await store.ListForUserAsync(userId, cancellationToken))
            booking.Anonymise();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
