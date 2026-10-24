namespace SmartRail.Modules.Journeys.Contracts;

// The next trains at some stations, with their live state, for a disruption that names those stations.
public interface IAffectedJourneys
{
    // At most `count` trains across the stations, soonest first, each train once.
    Task<IReadOnlyList<AffectedJourney>> FindAsync(IReadOnlyCollection<string> stationCodes, int count, string? language, CancellationToken cancellationToken);
}

public sealed record AffectedJourney(
    Guid JourneyId,
    string TrainNumber,
    string StationCode,
    string DestinationCode,
    string DestinationName,
    DateTimeOffset ScheduledDepartureUtc,
    DateTimeOffset ExpectedDepartureUtc,
    string Status,
    int? DelayMinutes,
    string? Platform);
