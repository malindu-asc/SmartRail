using FluentValidation;
using SmartRail.Application.Core.Messaging;

namespace SmartRail.Modules.Network.Application.Timetables.GetStationTimetable;

// Date is yyyy-MM-dd (empty means today in the network time zone). Kind is "departures" (default) or "arrivals".
public sealed record GetStationTimetableQuery(string Code, string? Date, string? Kind, string? Language)
    : IQuery<StationTimetableResponse>;

public sealed record StationTimetableResponse(
    string Language,
    string Date,
    string TimeZone,
    string Kind,
    TimetableStation Station,
    IReadOnlyList<TimetableItem> Items);

public sealed record TimetableStation(string Code, string Name);

// For departures the other station is the destination, for arrivals the origin.
public sealed record TimetableItem(
    Guid ServiceId,
    string TrainNumber,
    string RouteCode,
    string ScheduledTime,
    int Minutes,
    string OtherStationCode,
    string OtherStationName,
    string? Platform);

public sealed class GetStationTimetableQueryValidator : AbstractValidator<GetStationTimetableQuery>
{
    public GetStationTimetableQueryValidator()
    {
        RuleFor(x => x.Date)
            .Must(TimetableFormat.IsValidDateOrEmpty)
            .WithMessage("The date must be written yyyy-MM-dd.");

        RuleFor(x => x.Kind)
            .Must(TimetableFormat.IsValidKindOrEmpty)
            .WithMessage("The kind must be 'departures' or 'arrivals'.");
    }
}
