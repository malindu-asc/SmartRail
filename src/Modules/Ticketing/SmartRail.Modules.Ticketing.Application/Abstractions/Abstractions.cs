using SmartRail.Modules.Ticketing.Domain.Bookings;
using SmartRail.SharedKernel.Abstractions;

namespace SmartRail.Modules.Ticketing.Application.Abstractions;

public interface ITicketingUnitOfWork : IUnitOfWork
{
    // Starts a transaction that only one booking of this train can hold at a time, so two people cannot take the same seat.
    Task<ITransactionScope> LockJourneyAsync(Guid journeyId, CancellationToken cancellationToken);
}

public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}

public interface IBookingStore
{
    Task<Booking?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<Booking?> GetByCodeAsync(string bookingCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<Booking>> ListForUserAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Booking>> ListForJourneyAsync(Guid journeyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SeatHold>> ListHoldsAsync(Guid journeyId, CancellationToken cancellationToken);

    Task<SeatHold?> GetHoldAsync(Guid holdId, CancellationToken cancellationToken);

    Task<bool> ReferenceOrCodeExistsAsync(string reference, string bookingCode, CancellationToken cancellationToken);

    void Add(Booking booking);

    void Add(SeatHold hold);

    void Remove(SeatHold hold);
}

// Signs what the QR code carries, so a ticket can be checked without trusting the phone that shows it.
public interface ITicketSigner
{
    // "SR1.{ticketId}.{expiresUnix}.{signature}"
    string Sign(Booking booking);
}

public sealed class TicketingOptions
{
    public const string Section = "Ticketing";

    // Routes where a seat must be reserved (long-distance trains). Every other train is unreserved.
    public string[] SeatReservedRoutes { get; set; } = ["NORTH"];

    // How long chosen seats are kept while the passenger finishes (the "Seat held for 09:42" bar).
    public int SeatHoldMinutes { get; set; } = 10;
}
