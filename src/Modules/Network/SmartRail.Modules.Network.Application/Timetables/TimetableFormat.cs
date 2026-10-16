using System.Globalization;

namespace SmartRail.Modules.Network.Application.Timetables;

internal static class TimetableFormat
{
    internal const string Departures = "departures";
    internal const string Arrivals = "arrivals";

    // Minutes after midnight of the service date written HH:mm. After midnight the hours keep counting (24:25).
    internal static string Time(int minutes) => $"{minutes / 60:00}:{minutes % 60:00}";

    internal static bool TryParseDate(string? text, out DateOnly date) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    internal static bool IsValidDateOrEmpty(string? text) =>
        string.IsNullOrWhiteSpace(text) || TryParseDate(text.Trim(), out _);

    internal static bool IsValidKindOrEmpty(string? kind) =>
        string.IsNullOrWhiteSpace(kind)
        || string.Equals(kind.Trim(), Departures, StringComparison.OrdinalIgnoreCase)
        || string.Equals(kind.Trim(), Arrivals, StringComparison.OrdinalIgnoreCase);
}
