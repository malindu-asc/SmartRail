using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartRail.Modules.Ticketing.Application.Abstractions;
using SmartRail.Modules.Ticketing.Domain.Bookings;

namespace SmartRail.Modules.Ticketing.Infrastructure.Persistence;

internal sealed class BookingStore(TicketingDbContext context) : IBookingStore
{
    public Task<Booking?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Booking?> GetByCodeAsync(string bookingCode, CancellationToken cancellationToken) =>
        context.Bookings.FirstOrDefaultAsync(b => b.BookingCode == bookingCode, cancellationToken);

    public async Task<IReadOnlyList<Booking>> ListForUserAsync(string userId, CancellationToken cancellationToken) =>
        await context.Bookings.Where(b => b.UserId == userId).OrderBy(b => b.DepartureUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Booking>> ListForJourneyAsync(Guid journeyId, CancellationToken cancellationToken) =>
        await context.Bookings.AsNoTracking().Where(b => b.JourneyId == journeyId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SeatHold>> ListHoldsAsync(Guid journeyId, CancellationToken cancellationToken) =>
        await context.SeatHolds.Where(h => h.JourneyId == journeyId).ToListAsync(cancellationToken);

    public Task<SeatHold?> GetHoldAsync(Guid holdId, CancellationToken cancellationToken) =>
        context.SeatHolds.FirstOrDefaultAsync(h => h.Id == holdId, cancellationToken);

    public Task<bool> ReferenceOrCodeExistsAsync(string reference, string bookingCode, CancellationToken cancellationToken) =>
        context.Bookings.AnyAsync(b => b.Reference == reference || b.BookingCode == bookingCode, cancellationToken);

    public void Add(Booking booking) => context.Bookings.Add(booking);

    public void Add(SeatHold hold) => context.SeatHolds.Add(hold);

    public void Remove(SeatHold hold) => context.SeatHolds.Remove(hold);
}

public sealed class TicketSigningOptions
{
    public const string Section = "Ticketing:Signing";

    // Development key. Replace it outside development (it is a secret).
    public string Key { get; set; } = "dev-only-ticket-signing-key-change-me";
}

// "SR1.{ticketId}.{expiresUnix}.{signature}": a ticket is valid until a day after the train arrives.
internal sealed class HmacTicketSigner(IOptions<TicketSigningOptions> options) : ITicketSigner
{
    public string Sign(Booking booking)
    {
        var expires = booking.ArrivalUtc.AddDays(1).ToUnixTimeSeconds();
        var data = $"{booking.Id:N}|{booking.Reference}|{booking.JourneyId:N}|{booking.FromStationCode}|{booking.ToStationCode}|{expires}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.Value.Key));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return $"SR1.{booking.Id:N}.{expires}.{signature}";
    }
}
