using System.Text.RegularExpressions;
using SmartRail.Modules.Network.Domain.Errors;
using SmartRail.Modules.Network.Domain.Routes;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Domain.Timetables;

// A run pattern within one timetable version: a route, a calendar and ordered stops with explicit times.
// The train number is only a label (REQ-LIVE-019); the id is the identity.
public sealed partial class Service : Entity<Guid>
{
    public const int MaxMinutes = 2879;   // under 48 hours after midnight of the service date

    private readonly List<ServiceStop> _stops = [];

    private Service() { }

    private Service(Guid id) : base(id) { }

    public Guid TimetableVersionId { get; private set; }
    public Guid RouteId { get; private set; }
    public Guid CalendarId { get; private set; }
    public string TrainNumber { get; private set; } = string.Empty;
    public ServiceDirection Direction { get; private set; }
    public ServiceCategory Category { get; private set; }
    public IReadOnlyList<ServiceStop> Stops => _stops;

    public static Result<Service> Create(
        Guid timetableVersionId,
        Route route,
        Guid calendarId,
        string trainNumber,
        ServiceDirection direction,
        ServiceCategory category,
        IReadOnlyList<ServiceStopDraft> stops,
        Guid? id = null)
    {
        if (!TrainNumberPattern().IsMatch(trainNumber))
            return NetworkErrors.InvalidTrainNumber;

        if (stops.Count < 2)
            return NetworkErrors.ServiceNeedsTwoStops;

        for (var i = 0; i < stops.Count; i++)
        {
            if (stops[i].Sequence != i + 1)
                return NetworkErrors.ServiceSequenceInvalid;
        }

        if (stops.Select(s => s.StationId).Distinct().Count() != stops.Count)
            return NetworkErrors.ServiceStationRepeated;

        if (!FollowsRoute(route, direction, stops))
            return NetworkErrors.ServiceStopsNotOnRoute;

        if (stops[0].Kind == StopKind.Passing || stops[^1].Kind == StopKind.Passing)
            return NetworkErrors.ServiceEndpointPassing;

        var times = ValidateTimes(stops);
        if (times.IsFailure)
            return times.Error;

        var service = new Service(id ?? Guid.NewGuid())
        {
            TimetableVersionId = timetableVersionId,
            RouteId = route.Id,
            CalendarId = calendarId,
            TrainNumber = trainNumber,
            Direction = direction,
            Category = category
        };
        service._stops.AddRange(stops.Select(s => new ServiceStop(service.Id, s)));
        return service;
    }

    // New times for the same stops (a draft edit). The stops and their order do not change; the same rules as a new service apply.
    public Result ChangeTimes(IReadOnlyList<ServiceStopTime> times)
    {
        if (times.Count != _stops.Count)
            return NetworkErrors.StopTimesMismatch;

        var drafts = _stops.OrderBy(s => s.Sequence).Select((s, i) =>
            new ServiceStopDraft(s.Sequence, s.StationId, times[i].ArrivalMinutes, times[i].DepartureMinutes, s.DefaultPlatformId, s.Kind)).ToList();

        var valid = ValidateTimes(drafts);
        if (valid.IsFailure)
            return valid;

        foreach (var stop in _stops.OrderBy(s => s.Sequence).Zip(drafts))
            stop.First.SetTimes(stop.Second.ArrivalMinutes, stop.Second.DepartureMinutes);

        return Result.Success();
    }

    // The stops may skip stations (an express) but never reorder them.
    private static bool FollowsRoute(Route route, ServiceDirection direction, IReadOnlyList<ServiceStopDraft> stops)
    {
        var ordered = route.Stops.OrderBy(s => s.Sequence).Select(s => s.StationId).ToList();
        if (direction == ServiceDirection.Reverse)
            ordered.Reverse();

        var position = -1;
        foreach (var stop in stops)
        {
            var found = ordered.IndexOf(stop.StationId);
            if (found <= position)
                return false;

            position = found;
        }

        return true;
    }

    private static Result ValidateTimes(IReadOnlyList<ServiceStopDraft> stops)
    {
        var last = stops.Count - 1;

        for (var i = 0; i < stops.Count; i++)
        {
            var stop = stops[i];

            if ((stop.ArrivalMinutes is < 0 or > MaxMinutes) || (stop.DepartureMinutes is < 0 or > MaxMinutes))
                return NetworkErrors.ServiceTimeOutOfRange;

            if (i == 0 && (stop.ArrivalMinutes is not null || stop.DepartureMinutes is null))
                return NetworkErrors.ServiceTimesInvalid;

            if (i == last && (stop.DepartureMinutes is not null || stop.ArrivalMinutes is null))
                return NetworkErrors.ServiceTimesInvalid;

            if (i > 0 && i < last && (stop.ArrivalMinutes is null || stop.DepartureMinutes is null))
                return NetworkErrors.ServiceTimesInvalid;

            if (stop.ArrivalMinutes is { } arrival && stop.DepartureMinutes is { } departure && arrival > departure)
                return NetworkErrors.ServiceTimesInvalid;

            if (i > 0)
            {
                var previous = stops[i - 1].DepartureMinutes ?? stops[i - 1].ArrivalMinutes ?? 0;
                var current = stop.ArrivalMinutes ?? stop.DepartureMinutes ?? 0;
                if (current < previous)
                    return NetworkErrors.ServiceTimesInvalid;
            }
        }

        return Result.Success();
    }

    [GeneratedRegex("^[A-Za-z0-9]{1,10}$")]
    private static partial Regex TrainNumberPattern();
}

// Key: (ServiceId, Sequence). Times are minutes after midnight of the service date and may pass 1440.
public sealed class ServiceStop
{
    private ServiceStop() { }

    internal ServiceStop(Guid serviceId, ServiceStopDraft draft)
    {
        ServiceId = serviceId;
        Sequence = draft.Sequence;
        StationId = draft.StationId;
        ArrivalMinutes = draft.ArrivalMinutes;
        DepartureMinutes = draft.DepartureMinutes;
        DefaultPlatformId = draft.DefaultPlatformId;
        Kind = draft.Kind;
    }

    internal void SetTimes(int? arrival, int? departure)
    {
        ArrivalMinutes = arrival;
        DepartureMinutes = departure;
    }

    public Guid ServiceId { get; private set; }
    public int Sequence { get; private set; }
    public Guid StationId { get; private set; }
    public int? ArrivalMinutes { get; private set; }
    public int? DepartureMinutes { get; private set; }
    public Guid? DefaultPlatformId { get; private set; }
    public StopKind Kind { get; private set; }
}

public sealed record ServiceStopDraft(
    int Sequence,
    Guid StationId,
    int? ArrivalMinutes,
    int? DepartureMinutes,
    Guid? DefaultPlatformId,
    StopKind Kind);

// New times for one stop, in order: the first stop has only a departure, the last only an arrival.
public sealed record ServiceStopTime(int? ArrivalMinutes, int? DepartureMinutes);
