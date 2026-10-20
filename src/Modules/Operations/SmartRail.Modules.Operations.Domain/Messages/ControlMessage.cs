using SmartRail.Modules.Operations.Domain.Errors;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Operations.Domain.Messages;

// Sent: the controller sent it. Delivered: the driver's app fetched it. Acknowledged: the driver confirmed it (REQ-OPS-013).
public enum MessageState
{
    Sent = 0,
    Delivered = 1,
    Acknowledged = 2
}

// A short plain-text message from a controller to one driver, optionally needing an acknowledgement.
public sealed class ControlMessage : Entity<Guid>
{
    private ControlMessage() { }

    private ControlMessage(Guid id) : base(id) { }

    public string DriverSubject { get; private set; } = string.Empty;
    public Guid? JourneyId { get; private set; }
    public string? TrainNumber { get; private set; }
    public string? UnitCode { get; private set; }
    public Guid? IncidentId { get; private set; }
    public string? Template { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public bool RequiresAck { get; private set; }
    public DateTimeOffset ValidUntilUtc { get; private set; }
    public string SentBy { get; private set; } = string.Empty;
    public DateTimeOffset SentAtUtc { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }
    public DateTimeOffset? AcknowledgedAtUtc { get; private set; }
    public int ResentCount { get; private set; }

    public static Result<ControlMessage> Send(
        string driverSubject, Guid? journeyId, string? trainNumber, string? unitCode, Guid? incidentId, string? template, string body,
        bool requiresAck, DateTimeOffset validUntil, string sentBy, DateTimeOffset now, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(driverSubject))
            return OperationsErrors.RecipientRequired;
        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > 500)
            return OperationsErrors.MessageTextInvalid;
        if (validUntil <= now)
            return OperationsErrors.ValidUntilInvalid;

        return new ControlMessage(id ?? Guid.NewGuid())
        {
            DriverSubject = driverSubject.Trim(),
            JourneyId = journeyId,
            TrainNumber = trainNumber,
            UnitCode = unitCode,
            IncidentId = incidentId,
            Template = template,
            Body = body.Trim(),
            RequiresAck = requiresAck,
            ValidUntilUtc = validUntil,
            SentBy = sentBy,
            SentAtUtc = now
        };
    }

    public MessageState StateAt(DateTimeOffset now) =>
        AcknowledgedAtUtc is not null ? MessageState.Acknowledged : DeliveredAtUtc is not null ? MessageState.Delivered : MessageState.Sent;

    public bool IsExpired(DateTimeOffset now) => ValidUntilUtc <= now;

    // Needs an acknowledgement that has not come, a little after it was sent. The controller is told to try the radio.
    public bool IsOverdue(DateTimeOffset now, TimeSpan grace) =>
        RequiresAck && AcknowledgedAtUtc is null && !IsExpired(now) && now - SentAtUtc >= grace;

    // The driver's app fetched it.
    public void MarkDelivered(DateTimeOffset now) => DeliveredAtUtc ??= now;

    public Result Acknowledge(DateTimeOffset now)
    {
        if (IsExpired(now))
            return OperationsErrors.MessageExpired;

        DeliveredAtUtc ??= now;
        AcknowledgedAtUtc ??= now;
        return Result.Success();
    }

    // Sent again: it counts as a new delivery from now on.
    public Result Resend(DateTimeOffset now, DateTimeOffset validUntil)
    {
        if (AcknowledgedAtUtc is not null)
            return Result.Success();
        if (validUntil <= now)
            return OperationsErrors.ValidUntilInvalid;

        SentAtUtc = now;
        DeliveredAtUtc = null;
        ValidUntilUtc = validUntil;
        ResentCount++;
        return Result.Success();
    }
}

public enum AssistanceStatus
{
    Open = 0,
    Handled = 1
}

// A driver asks for help (an unwell passenger, a safety worry). It appears on the controller's dashboard until someone handles it.
public sealed class AssistanceRequest : Entity<Guid>
{
    private AssistanceRequest() { }

    private AssistanceRequest(Guid id) : base(id) { }

    public string DriverSubject { get; private set; } = string.Empty;
    public Guid? JourneyId { get; private set; }
    public string? TrainNumber { get; private set; }
    public string? StationCode { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset RequestedAtUtc { get; private set; }
    public AssistanceStatus Status { get; private set; }
    public string? HandledBy { get; private set; }
    public DateTimeOffset? HandledAtUtc { get; private set; }
    public string? Outcome { get; private set; }

    public static AssistanceRequest Create(string driverSubject, Guid? journeyId, string? trainNumber, string? stationCode, string? note, DateTimeOffset now, Guid? id = null) =>
        new(id ?? Guid.NewGuid())
        {
            DriverSubject = driverSubject,
            JourneyId = journeyId,
            TrainNumber = trainNumber,
            StationCode = stationCode?.Trim().ToUpperInvariant(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(note.Trim().Length, 300)],
            RequestedAtUtc = now,
            Status = AssistanceStatus.Open
        };

    public Result Handle(string by, string? outcome, DateTimeOffset now)
    {
        if (Status == AssistanceStatus.Handled)
            return OperationsErrors.AssistanceClosed;

        Status = AssistanceStatus.Handled;
        HandledBy = by;
        HandledAtUtc = now;
        Outcome = string.IsNullOrWhiteSpace(outcome) ? null : outcome.Trim()[..Math.Min(outcome.Trim().Length, 300)];
        return Result.Success();
    }
}
