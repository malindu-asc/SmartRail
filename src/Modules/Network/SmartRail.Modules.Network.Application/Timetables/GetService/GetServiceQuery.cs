using FluentValidation;
using SmartRail.Application.Core.Messaging;

namespace SmartRail.Modules.Network.Application.Timetables.GetService;

public sealed record GetServiceQuery(Guid ServiceId, string? Date, string? Language) : IQuery<ServiceResponse>;

public sealed record ServiceResponse(
    string Language,
    string Date,
    string TimeZone,
    Guid ServiceId,
    string TrainNumber,
    string RouteCode,
    IReadOnlyList<ServiceStopResponse> Stops);

public sealed record ServiceStopResponse(
    int Sequence,
    string StationCode,
    string StationName,
    string? ScheduledArrival,
    string? ScheduledDeparture,
    int? ArrivalMinutes,
    int? DepartureMinutes,
    string? Platform,
    string Kind);

public sealed class GetServiceQueryValidator : AbstractValidator<GetServiceQuery>
{
    public GetServiceQueryValidator()
    {
        RuleFor(x => x.Date)
            .Must(TimetableFormat.IsValidDateOrEmpty)
            .WithMessage("The date must be written yyyy-MM-dd.");
    }
}
