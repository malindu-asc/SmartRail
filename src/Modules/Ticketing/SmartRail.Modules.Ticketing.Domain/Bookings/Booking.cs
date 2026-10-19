using SmartRail.SharedKernel.Primitives;

namespace SmartRail.Modules.Ticketing.Domain.Bookings;

// A seat in a coach, for example coach B seat 13.
public sealed record SeatRef(string Coach, int Number)
{
    public override string ToString() => $"{Coach}{Number}";
}

// A confirmed booking: the ticket for one train between two stops, for one to nine people. No payment is taken in the MVP
// (SPEC-022): the fare is stored as information. The ticket is bound to the journey id, so a delay never creates a new ticket.
public sealed class Booking : Entity<Guid>
{
    private Booking() { }

    public Booking(
        Guid id, string reference, string bookingCode, string? userId, string? contactEmail,
        Guid journeyId, DateOnly serviceDate, string routeCode, string trainNumber,
        string fromStationCode, string toStationCode, int fromIndex, int toIndex,
        DateTimeOffset departureUtc, DateTimeOffset arrivalUtc,
        int adults, int children, int seniors, bool needsHelp,
        decimal fareTotal, string currency, IReadOnlyList<SeatRef> seats, DateTimeOffset now)
        : base(id)
    {
        Reference = reference;
        BookingCode = bookingCode;
        UserId = userId;
        ContactEmail = contactEmail;
        JourneyId = journeyId;
        ServiceDate = serviceDate;
        RouteCode = routeCode;
        TrainNumber = trainNumber;
        FromStationCode = fromStationCode;
        ToStationCode = toStationCode;
        FromIndex = fromIndex;
        ToIndex = toIndex;
        DepartureUtc = departureUtc;
        ArrivalUtc = arrivalUtc;
        Adults = adults;
        Children = children;
        Seniors = seniors;
        NeedsHelp = needsHelp;
        FareTotal = fareTotal;
        Currency = currency;
        Seats = seats.ToList();
        CreatedAtUtc = now;
    }

    // The number on the ticket, for example SR-8821-0097.
    public string Reference { get; private set; } = string.Empty;

    // Eight characters from the confirmation, used to add the ticket on another phone.
    public string BookingCode { get; private set; } = string.Empty;

    // The signed-in passenger (Keycloak subject). Null after "Delete my data": the record stays, the person does not.
    public string? UserId { get; private set; }

    public string? ContactEmail { get; private set; }

    public Guid JourneyId { get; private set; }
    public DateOnly ServiceDate { get; private set; }
    public string RouteCode { get; private set; } = string.Empty;
    public string TrainNumber { get; private set; } = string.Empty;
    public string FromStationCode { get; private set; } = string.Empty;
    public string ToStationCode { get; private set; } = string.Empty;

    // Positions of the two stops in the journey's stop list, so seat use on overlapping parts of a trip can be told apart.
    public int FromIndex { get; private set; }
    public int ToIndex { get; private set; }

    public DateTimeOffset DepartureUtc { get; private set; }
    public DateTimeOffset ArrivalUtc { get; private set; }
    public int Adults { get; private set; }
    public int Children { get; private set; }
    public int Seniors { get; private set; }
    public bool NeedsHelp { get; private set; }
    public decimal FareTotal { get; private set; }
    public string Currency { get; private set; } = "LKR";
    public List<SeatRef> Seats { get; private set; } = [];
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public int Passengers => Adults + Children + Seniors;

    // Adds the ticket to this person (another phone, a lost session).
    public void AssignTo(string userId, string? contactEmail)
    {
        UserId = userId;
        ContactEmail = contactEmail ?? ContactEmail;
    }

    // "Delete my data": the booking stays as a record of the sale, with nobody attached to it.
    public void Anonymise()
    {
        UserId = null;
        ContactEmail = null;
    }
}

// Seats held while the passenger finishes the booking. They free themselves when the hold runs out.
public sealed class SeatHold : Entity<Guid>
{
    private SeatHold() { }

    public SeatHold(Guid id, string userId, Guid journeyId, int fromIndex, int toIndex, IReadOnlyList<SeatRef> seats, DateTimeOffset expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        JourneyId = journeyId;
        FromIndex = fromIndex;
        ToIndex = toIndex;
        Seats = seats.ToList();
        ExpiresAtUtc = expiresAtUtc;
    }

    public string UserId { get; private set; } = string.Empty;
    public Guid JourneyId { get; private set; }
    public int FromIndex { get; private set; }
    public int ToIndex { get; private set; }
    public List<SeatRef> Seats { get; private set; } = [];
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public bool IsActive(DateTimeOffset now) => ExpiresAtUtc > now;

    // True when the two trips share any part of the line (each trip is the stretch from its from-index up to its to-index).
    public static bool Overlaps(int fromA, int toA, int fromB, int toB) => fromA < toB && fromB < toA;
}
