namespace SmartRail.Modules.Journeys.Domain.Live;

// One raw position from a feed source, kept for 7 days (REQ-DATA-003). ResolvedJourneyId is null for a movement that
// matches no journey (a phantom or ghost movement): it is stored, counted, and never shown publicly (REQ-LIVE-015).
public sealed class TelemetryReading
{
    private TelemetryReading() { }

    public TelemetryReading(
        Guid sourceId,
        string vehicleCode,
        DateTimeOffset recordedAt,
        DateTimeOffset receivedAt,
        long sequence,
        string? sectionFromStationCode,
        string? sectionToStationCode,
        decimal? progress,
        decimal? speedKmh,
        Guid? resolvedJourneyId,
        bool accepted,
        string? rejectReason)
    {
        SourceId = sourceId;
        VehicleCode = vehicleCode;
        RecordedAt = recordedAt;
        ReceivedAt = receivedAt;
        Sequence = sequence;
        SectionFromStationCode = sectionFromStationCode;
        SectionToStationCode = sectionToStationCode;
        Progress = progress;
        SpeedKmh = speedKmh;
        ResolvedJourneyId = resolvedJourneyId;
        Accepted = accepted;
        RejectReason = rejectReason;
    }

    public long Id { get; private set; }
    public Guid SourceId { get; private set; }
    public string VehicleCode { get; private set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public long Sequence { get; private set; }
    public string? SectionFromStationCode { get; private set; }
    public string? SectionToStationCode { get; private set; }
    public decimal? Progress { get; private set; }
    public decimal? SpeedKmh { get; private set; }
    public Guid? ResolvedJourneyId { get; private set; }
    public bool Accepted { get; private set; }
    public string? RejectReason { get; private set; }
    public bool LegalHold { get; private set; }
}
