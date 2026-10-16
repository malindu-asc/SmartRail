using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Queries.GetJourney;

public sealed class GetJourneyQueryHandler(
    IJourneyReader reader,
    INetworkStations stations,
    INetworkTimeZone timeZone)
    : IQueryHandler<GetJourneyQuery, JourneyResponse>
{
    public async Task<Result<JourneyResponse>> Handle(GetJourneyQuery query, CancellationToken cancellationToken)
    {
        var journey = await reader.GetJourneyAsync(query.JourneyId, cancellationToken);
        if (journey is null)
            return JourneysErrors.JourneyNotFound(query.JourneyId);

        // Public view: passing points are removed and the sequence is renumbered.
        var publicStops = journey.Stops.Where(s => s.Kind != JourneyStopKind.Passing).OrderBy(s => s.Sequence).ToList();
        var names = await stations.GetNamesAsync(publicStops.Select(s => s.StationCode), query.Language, cancellationToken);
        string Name(string code) => names.TryGetValue(code, out var n) ? n : code;

        var stops = publicStops.Select((s, index) => new JourneyStopResponse(
            index + 1,
            s.StationCode,
            Name(s.StationCode),
            s.State.ToString(),
            s.ScheduledArrivalUtc,
            s.ScheduledDepartureUtc,
            s.PlatformCode,
            s.PlatformState.ToString())).ToList();

        return new JourneyResponse(
            journey.Id,
            journey.ServiceDate.ToString("yyyy-MM-dd"),
            timeZone.TimeZoneId,
            journey.TrainNumber,
            journey.RouteCode,
            JourneyLifecycleRules.PublicStatus(journey.Lifecycle),
            journey.Confidence.ToString(),
            new JourneyEnd(journey.OriginStationCode, Name(journey.OriginStationCode)),
            new JourneyEnd(journey.DestinationStationCode, Name(journey.DestinationStationCode)),
            null,
            stops);
    }
}
