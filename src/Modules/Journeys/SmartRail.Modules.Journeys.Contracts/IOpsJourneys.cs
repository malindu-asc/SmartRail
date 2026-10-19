namespace SmartRail.Modules.Journeys.Contracts;

// What the dashboards and incidents (Operations module) may ask and do through the Journeys module.
public interface IOpsJourneyStats
{
    Task<OpsStats> GetAsync(CancellationToken cancellationToken);

    // Journey-level things that need a person: no live data, no driver or unit soon, a unit out of service, a major delay.
    Task<IReadOnlyList<OpsAttentionItem>> GetAttentionAsync(CancellationToken cancellationToken);

    // How many trains call at these stations in the next hours (used to size a disruption: "6 journeys affected in the next 2 hours").
    Task<int> CountAffectedAsync(IReadOnlyCollection<string> stationCodes, int hours, CancellationToken cancellationToken);

    // What other modules need to know about one journey (the driver to message, the stations to warn, the train number).
    Task<OpsJourneyFacts?> GetFactsAsync(Guid journeyId, CancellationToken cancellationToken);

    // The next journeys at the stations, for the passenger preview and the incident page.
    Task<IReadOnlyList<OpsJourneyBrief>> ListAffectedAsync(IReadOnlyCollection<string> stationCodes, int hours, int take, CancellationToken cancellationToken);
}

public sealed record OpsStats(
    DateOnly ServiceDate,
    DateTimeOffset AsOfUtc,
    int Running,
    int? OnTimePercent,
    int Delayed,
    int DelayedOver15,
    int Cancelled,
    int NoLiveData,
    int Completed,
    int Total);

// Kind: NoLiveData, NoDriver, NoUnit, UnitOutOfService, MajorDelay, Interrupted. Action: Open or Assign.
public sealed record OpsAttentionItem(string Kind, Guid JourneyId, string Title, string Detail, string Action, int Severity);

public sealed record OpsJourneyBrief(Guid JourneyId, string TrainNumber, string RouteCode, string Origin, string Destination, DateTimeOffset DepartsUtc, string Status);

public sealed record OpsJourneyFacts(
    Guid JourneyId,
    string TrainNumber,
    string RouteCode,
    string Lifecycle,
    DateTimeOffset ScheduledDepartureUtc,
    IReadOnlyList<string> StationCodes,
    string? DriverSubject,
    string? UnitCode,
    string? NextStopCode);

// Changing a journey from another module. The Journeys module still checks the rules and writes the audit row.
public interface IJourneyControl
{
    Task<JourneyControlResult> TerminateEmergencyAsync(Guid journeyId, string reason, CancellationToken cancellationToken);
}

public sealed record JourneyControlResult(bool Succeeded, string? ErrorCode, string? Message);
