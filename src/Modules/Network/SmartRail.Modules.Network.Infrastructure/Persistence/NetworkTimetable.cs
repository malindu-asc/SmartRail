using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Contracts;

namespace SmartRail.Modules.Network.Infrastructure.Persistence;

// The Contracts implementation Journeys will call. Working view: passing points are included.
internal sealed class NetworkTimetable(ITimetableReader reader) : INetworkTimetable
{
    public async Task<IReadOnlyList<ScheduledService>> GetServicesRunningOnAsync(DateOnly serviceDate, CancellationToken cancellationToken)
    {
        var day = await reader.GetDayAsync(serviceDate, cancellationToken);
        if (day is null)
            return [];

        return day.Services
            .Select(s => new ScheduledService(
                s.ServiceId,
                day.VersionId,
                s.TrainNumber,
                s.RouteCode,
                s.Stops.Select(st => new ScheduledStop(
                    st.Sequence,
                    st.StationCode,
                    st.ArrivalMinutes,
                    st.DepartureMinutes,
                    st.PlatformCode,
                    st.Kind)).ToList()))
            .ToList();
    }
}
