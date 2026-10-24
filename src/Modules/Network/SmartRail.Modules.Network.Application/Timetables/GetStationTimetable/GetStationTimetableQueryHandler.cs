using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Application.Languages;
using SmartRail.Modules.Network.Domain.Errors;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Application.Timetables.GetStationTimetable;

public sealed class GetStationTimetableQueryHandler(
    INetworkReader network,
    ITimetableReader timetable,
    INetworkSettings settings)
    : IQueryHandler<GetStationTimetableQuery, StationTimetableResponse>
{
    public async Task<Result<StationTimetableResponse>> Handle(GetStationTimetableQuery query, CancellationToken cancellationToken)
    {
        var station = await network.GetActiveStationAsync(query.Code, cancellationToken);
        if (station is null)
            return NetworkErrors.StationNotFound(query.Code);

        var date = TimetableFormat.TryParseDate(query.Date?.Trim(), out var parsed) ? parsed : settings.Today();
        var arrivals = string.Equals(query.Kind?.Trim(), TimetableFormat.Arrivals, StringComparison.OrdinalIgnoreCase);

        var day = await timetable.GetDayAsync(date, cancellationToken);
        if (day is null)
            return NetworkErrors.NoTimetableForDate(date);

        var languages = await network.GetLanguagesAsync(cancellationToken);
        var language = LanguageResolver.Resolve(query.Language, languages);

        var items = new List<TimetableItem>();
        foreach (var service in day.Services)
        {
            var here = service.Stops.FirstOrDefault(s =>
                s.StationCode == station.Code && s.Kind != "Passing");
            if (here is null)
                continue;

            // Boarding is allowed at a stop unless it is set-down only; alighting unless it is pick-up only.
            var minutes = arrivals
                ? (here.Kind == "PickUpOnly" ? null : here.ArrivalMinutes)
                : (here.Kind == "SetDownOnly" ? null : here.DepartureMinutes);
            if (minutes is null)
                continue;

            var publicStops = service.Stops.Where(s => s.Kind != "Passing").OrderBy(s => s.Sequence).ToList();
            var other = arrivals ? publicStops[0] : publicStops[^1];

            items.Add(new TimetableItem(
                service.ServiceId,
                service.TrainNumber,
                service.RouteCode,
                TimetableFormat.Time(minutes.Value),
                minutes.Value,
                other.StationCode,
                LanguageResolver.Localize(other.StationName, other.StationTranslations, language),
                here.PlatformCode));
        }

        items = items.OrderBy(i => i.Minutes).ThenBy(i => i.TrainNumber, StringComparer.Ordinal).ToList();

        return new StationTimetableResponse(
            language,
            date.ToString("yyyy-MM-dd"),
            settings.TimeZoneId,
            arrivals ? TimetableFormat.Arrivals : TimetableFormat.Departures,
            new TimetableStation(station.Code, LanguageResolver.Localize(station.Name, station.Translations, language)),
            items);
    }
}
