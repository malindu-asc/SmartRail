using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Queries.GetJourney;

public sealed class GetJourneyQueryHandler(
    IJourneyReader reader,
    INetworkStations stations,
    INetworkTimeZone timeZone,
    TimeProvider clock,
    LiveRules rules)
    : IQueryHandler<GetJourneyQuery, JourneyResponse>
{
    public async Task<Result<JourneyResponse>> Handle(GetJourneyQuery query, CancellationToken cancellationToken)
    {
        var journey = await reader.GetJourneyAsync(query.JourneyId, cancellationToken);
        if (journey is null)
            return JourneysErrors.JourneyNotFound(query.JourneyId);

        var status = LiveStatus.Evaluate(journey.Lifecycle, journey.Confidence, journey.LastAcceptedUpdateAt, clock.GetUtcNow(), rules);

        // Public view: passing points are removed and the sequence is renumbered.
        var publicStops = journey.Stops.Where(s => s.Kind != JourneyStopKind.Passing).OrderBy(s => s.Sequence).ToList();
        var names = await stations.GetNamesAsync(publicStops.Select(s => s.StationCode), query.Language, cancellationToken);
        string Name(string code) => names.TryGetValue(code, out var n) ? n : code;

        var stops = publicStops.Select((s, index) =>
        {
            var happened = s.State is JourneyStopState.Arrived or JourneyStopState.Departed;
            var live = status.ShowLiveTimes && !happened;

            return new JourneyStopResponse(
                index + 1,
                s.StationCode,
                Name(s.StationCode),
                s.State.ToString(),
                s.ScheduledArrivalUtc,
                s.ScheduledDepartureUtc,
                live ? s.EstimatedArrivalUtc : null,
                live ? s.EstimatedDepartureUtc : null,
                s.ActualArrivalUtc,
                s.ActualDepartureUtc,
                (happened ? EstimateSource.Confirmed : live && s.EstimatedDepartureUtc is not null ? EstimateSource.Live : EstimateSource.Scheduled).ToString(),
                s.PlatformCode,
                s.PlatformState.ToString());
        }).ToList();

        return new JourneyResponse(
            journey.Id,
            journey.ServiceDate.ToString("yyyy-MM-dd"),
            timeZone.TimeZoneId,
            journey.TrainNumber,
            journey.RouteCode,
            JourneyLifecycleRules.PublicStatus(journey.Lifecycle),
            status.Confidence.ToString(),
            new JourneyEnd(journey.OriginStationCode, Name(journey.OriginStationCode)),
            new JourneyEnd(journey.DestinationStationCode, Name(journey.DestinationStationCode)),
            status.ShowLiveTimes ? journey.DelayMinutes : null,
            status.ShowLiveTimes ? journey.Punctuality?.ToString() : null,
            status.IsStale,
            status.AgeSeconds,
            journey.LastAcceptedUpdateAt,
            stops);
    }
}
