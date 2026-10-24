using System.Text.RegularExpressions;
using SmartRail.Modules.Network.Domain.Errors;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Domain.Timetables;

// Days of the week a service runs, a valid range, and exception dates (holidays removed, specials added).
public sealed partial class ServiceCalendar : Entity<Guid>
{
    private readonly List<CalendarException> _exceptions = [];

    private ServiceCalendar() { }

    private ServiceCalendar(Guid id) : base(id) { }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool Monday { get; private set; }
    public bool Tuesday { get; private set; }
    public bool Wednesday { get; private set; }
    public bool Thursday { get; private set; }
    public bool Friday { get; private set; }
    public bool Saturday { get; private set; }
    public bool Sunday { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public IReadOnlyList<CalendarException> Exceptions => _exceptions;

    public static Result<ServiceCalendar> Create(
        string code,
        string name,
        IReadOnlySet<DayOfWeek> days,
        DateOnly validFrom,
        DateOnly? validTo,
        Guid? id = null)
    {
        if (!CalendarCodePattern().IsMatch(code))
            return NetworkErrors.InvalidCalendarCode;

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            return NetworkErrors.InvalidName;

        if (days.Count == 0 || (validTo is not null && validTo < validFrom))
            return NetworkErrors.CalendarRangeInvalid;

        return new ServiceCalendar(id ?? Guid.NewGuid())
        {
            Code = code,
            Name = name.Trim(),
            Monday = days.Contains(DayOfWeek.Monday),
            Tuesday = days.Contains(DayOfWeek.Tuesday),
            Wednesday = days.Contains(DayOfWeek.Wednesday),
            Thursday = days.Contains(DayOfWeek.Thursday),
            Friday = days.Contains(DayOfWeek.Friday),
            Saturday = days.Contains(DayOfWeek.Saturday),
            Sunday = days.Contains(DayOfWeek.Sunday),
            ValidFrom = validFrom,
            ValidTo = validTo
        };
    }

    public Result AddException(DateOnly date, CalendarExceptionKind kind, string? reason)
    {
        if (_exceptions.Any(e => e.Date == date))
            return NetworkErrors.DuplicateCalendarException(date);

        _exceptions.Add(new CalendarException(Id, date, kind, reason));
        return Result.Success();
    }

    // An exception decides the day outright. Otherwise the weekday and the valid range decide.
    public bool RunsOn(DateOnly date)
    {
        var exception = _exceptions.FirstOrDefault(e => e.Date == date);
        if (exception is not null)
            return exception.Kind == CalendarExceptionKind.Added;

        if (date < ValidFrom || (ValidTo is not null && date > ValidTo))
            return false;

        return date.DayOfWeek switch
        {
            DayOfWeek.Monday => Monday,
            DayOfWeek.Tuesday => Tuesday,
            DayOfWeek.Wednesday => Wednesday,
            DayOfWeek.Thursday => Thursday,
            DayOfWeek.Friday => Friday,
            DayOfWeek.Saturday => Saturday,
            _ => Sunday
        };
    }

    [GeneratedRegex("^[A-Z]{2,20}$")]
    private static partial Regex CalendarCodePattern();
}

// Key: (CalendarId, Date).
public sealed class CalendarException
{
    private CalendarException() { }

    internal CalendarException(Guid calendarId, DateOnly date, CalendarExceptionKind kind, string? reason)
    {
        CalendarId = calendarId;
        Date = date;
        Kind = kind;
        Reason = reason;
    }

    public Guid CalendarId { get; private set; }
    public DateOnly Date { get; private set; }
    public CalendarExceptionKind Kind { get; private set; }
    public string? Reason { get; private set; }
}
