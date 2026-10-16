using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Application.Languages;
using SmartRail.Modules.Network.Domain.Errors;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Application.Timetables.GetService;

public sealed class GetServiceQueryHandler(
    INetworkReader network,
    ITimetableReader timetable,
    INetworkSettings settings)
    : IQueryHandler<GetServiceQuery, ServiceResponse>
{
    public async Task<Result<ServiceResponse>> Handle(GetServiceQuery query, CancellationToken cancellationToken)
    {
        if (!await timetable.ServiceExistsAsync(query.ServiceId, cancellationToken))
            return NetworkErrors.ServiceNotFound(query.ServiceId);

        var date = TimetableFormat.TryParseDate(query.Date?.Trim(), out var parsed) ? parsed : settings.Today();

        var day = await timetable.GetDayAsync(date, cancellationToken);
        if (day is null)
            return NetworkErrors.NoTimetableForDate(date);

        var service = day.Services.FirstOrDefault(s => s.ServiceId == query.ServiceId);
        if (service is null)
            return NetworkErrors.ServiceNotRunning(query.ServiceId, date);

        var languages = await network.GetLanguagesAsync(cancellationToken);
        var language = LanguageResolver.Resolve(query.Language, languages);

        // Public view: passing points are removed and the sequence is renumbered.
        var stops = service.Stops
            .Where(s => s.Kind != "Passing")
            .OrderBy(s => s.Sequence)
            .Select((s, index) => new ServiceStopResponse(
                index + 1,
                s.StationCode,
                LanguageResolver.Localize(s.StationName, s.StationTranslations, language),
                s.ArrivalMinutes is { } a ? TimetableFormat.Time(a) : null,
                s.DepartureMinutes is { } d ? TimetableFormat.Time(d) : null,
                s.ArrivalMinutes,
                s.DepartureMinutes,
                s.PlatformCode,
                s.Kind))
            .ToList();

        return new ServiceResponse(
            language,
            date.ToString("yyyy-MM-dd"),
            settings.TimeZoneId,
            service.ServiceId,
            service.TrainNumber,
            service.RouteCode,
            stops);
    }
}
