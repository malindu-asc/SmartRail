namespace SmartRail.Modules.Journeys.Contracts;

// What other modules (tickets, notifications) may ask the Journeys module. The journey id is the identity everywhere.
public interface IJourneyLookup
{
    // Null when no journey has this id.
    Task<JourneyRef?> FindAsync(Guid journeyId, CancellationToken cancellationToken);
}

// Lifecycle is the lifecycle name (Planned, Ready, Running, ...). Stop codes are in order, passing points included.
public sealed record JourneyRef(
    Guid JourneyId,
    DateOnly ServiceDate,
    string Lifecycle,
    IReadOnlyList<string> StationCodes);
