using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Ticketing.Domain.Bookings;

public static class TicketingErrors
{
    public static Error JourneyNotFound(Guid id) =>
        Error.NotFound("Ticketing.JourneyNotFound", $"The train '{id}' was not found between these stations.");

    public static readonly Error BookingNotFound =
        Error.NotFound("Ticketing.BookingNotFound", "This ticket was not found.");

    public static readonly Error CannotBookThisTrain =
        Error.Conflict("Ticketing.CannotBookThisTrain", "This train cannot be booked: it has left, finished or been cancelled.");

    public static readonly Error PassengerCount =
        Error.Validation("Ticketing.PassengerCount", "Choose between 1 and 9 travellers.");

    public static readonly Error SeatsNotNeeded =
        Error.Validation("Ticketing.SeatsNotNeeded", "This train has no seat reservation.");

    public static readonly Error SeatCount =
        Error.Validation("Ticketing.SeatCount", "Choose one seat for each traveller.");

    public static readonly Error SeatInvalid =
        Error.Validation("Ticketing.SeatInvalid", "That seat does not exist.");

    public static readonly Error SeatTaken =
        Error.Conflict("Ticketing.SeatTaken", "Someone just took that seat. Please choose another.");

    public static readonly Error HoldExpired =
        Error.Conflict("Ticketing.HoldExpired", "Your seats were released. Please choose them again.");

    public static readonly Error ClaimNotMatched =
        Error.NotFound("Ticketing.ClaimNotMatched", "We could not find a ticket with those details.");
}

// What a ticket costs (information only in the MVP). A fare grows with the minutes on the train, then the category changes it.
public sealed class FareOptions
{
    public const string Section = "Ticketing:Fares";

    public string Currency { get; set; } = "LKR";

    // 45 minutes on the train is LKR 320 with the defaults.
    public decimal Base { get; set; } = 50;

    public decimal PerMinute { get; set; } = 6;

    public decimal ChildFactor { get; set; } = 0.5m;

    public decimal SeniorFactor { get; set; } = 0.5m;

    public decimal Rounding { get; set; } = 10;
}

public sealed record FareLine(string Category, int Count, decimal Each, decimal Total);

public sealed record FareQuote(string Currency, IReadOnlyList<FareLine> Lines, decimal Total);

public static class FareCalculator
{
    public static FareQuote Quote(FareOptions options, int minutes, int adults, int children, int seniors)
    {
        decimal Price(decimal factor)
        {
            var raw = (options.Base + options.PerMinute * minutes) * factor;
            return Math.Round(raw / options.Rounding, MidpointRounding.AwayFromZero) * options.Rounding;
        }

        var lines = new List<FareLine>();
        void Add(string category, int count, decimal factor)
        {
            if (count > 0)
            {
                var each = Price(factor);
                lines.Add(new FareLine(category, count, each, each * count));
            }
        }

        Add("Adult", adults, 1m);
        Add("Child", children, options.ChildFactor);
        Add("Senior", seniors, options.SeniorFactor);

        return new FareQuote(options.Currency, lines, lines.Sum(l => l.Total));
    }
}

// The seats of a train with reservation: coaches A to C, 28 seats each (the layout of the seat map).
public static class SeatLayout
{
    public static readonly IReadOnlyList<string> Coaches = ["A", "B", "C"];

    public const int SeatsPerCoach = 28;

    public static bool Exists(SeatRef seat) => Coaches.Contains(seat.Coach) && seat.Number is >= 1 and <= SeatsPerCoach;
}

public enum TicketState
{
    Valid,

    // The railway cancelled or terminated the train: the ticket can no longer be used.
    Affected,

    Past
}

public static class TicketStates
{
    // An affected ticket stays in the active list for a day, so the passenger can see what happened.
    private static readonly TimeSpan AffectedStays = TimeSpan.FromHours(24);
    private static readonly TimeSpan PastAfterArrival = TimeSpan.FromMinutes(30);

    public static TicketState Evaluate(string journeyLifecycle, DateTimeOffset departureUtc, DateTimeOffset arrivalUtc, DateTimeOffset now)
    {
        if (journeyLifecycle is "Cancelled" or "TerminatedEmergency")
            return now - departureUtc > AffectedStays ? TicketState.Past : TicketState.Affected;

        return now - arrivalUtc > PastAfterArrival ? TicketState.Past : TicketState.Valid;
    }
}
