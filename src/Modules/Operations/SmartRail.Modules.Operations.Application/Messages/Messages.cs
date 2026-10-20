using SmartRail.Application.Core.Abstractions;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Journeys.Contracts;
using SmartRail.Modules.Network.Contracts;
using SmartRail.Modules.Operations.Application.Abstractions;
using SmartRail.Modules.Operations.Domain.Errors;
using SmartRail.Modules.Operations.Domain.Messages;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Operations.Application.Messages;

// ---- views ----

public sealed record MessageTemplate(string Code, string Label, string Body);

public sealed record MessageRow(
    Guid Id,
    string DriverSubject,
    string? DriverName,
    string? UnitCode,
    string? TrainNumber,
    Guid? JourneyId,
    string? Template,
    string Body,
    bool RequiresAck,
    DateTimeOffset ValidUntilUtc,
    DateTimeOffset SentAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    DateTimeOffset? AcknowledgedAtUtc,
    string State,
    bool Overdue,
    int? OverdueMinutes,
    bool Expired,
    int ResentCount,
    string SentBy);

public sealed record MessageList(int Open, IReadOnlyList<MessageRow> Items);

public static class MessageTemplates
{
    public static readonly IReadOnlyList<MessageTemplate> All =
    [
        new("hold_at_station", "Hold at station", "Hold the train at the station until told otherwise."),
        new("platform_change", "Platform change", "Your platform has changed. Check the platform shown on the console."),
        new("speed_restriction", "Speed restriction", "A speed restriction is in force on the section ahead."),
        new("restriction_cleared", "Speed restriction cleared", "The speed restriction has been cleared."),
        new("report_to_control", "Report to control", "Call control on the radio when you can."),
        new("custom", "Custom message", string.Empty)
    ];
}

internal static class MessageViews
{
    // Not acknowledged this long after sending, and the controller is told to try the radio.
    public static readonly TimeSpan OverdueAfter = TimeSpan.FromMinutes(2);

    public static MessageRow Row(ControlMessage m, IReadOnlyDictionary<string, string> people, DateTimeOffset now) =>
        new(m.Id, m.DriverSubject, people.GetValueOrDefault(m.DriverSubject), m.UnitCode, m.TrainNumber, m.JourneyId, m.Template, m.Body, m.RequiresAck,
            m.ValidUntilUtc, m.SentAtUtc, m.DeliveredAtUtc, m.AcknowledgedAtUtc, m.StateAt(now).ToString(), m.IsOverdue(now, OverdueAfter),
            m.IsOverdue(now, OverdueAfter) ? (int)(now - m.SentAtUtc).TotalMinutes : null, m.IsExpired(now), m.ResentCount, m.SentBy);
}

// ---- controller: what was sent today ----

public sealed record ListMessagesQuery(bool OpenOnly) : IQuery<MessageList>;

public sealed class ListMessagesQueryHandler(IMessageStore store, IStaffNames staff, INetworkTimeZone timeZone, TimeProvider clock)
    : IQueryHandler<ListMessagesQuery, MessageList>
{
    public async Task<Result<MessageList>> Handle(ListMessagesQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var all = await store.ListSinceAsync(timeZone.ToUtc(timeZone.Today(), 0), cancellationToken);
        var people = await staff.GetNamesAsync(all.Select(m => m.DriverSubject).Distinct(), cancellationToken);

        var open = all.Count(m => m.RequiresAck && m.AcknowledgedAtUtc is null && !m.IsExpired(now));
        var shown = query.OpenOnly ? all.Where(m => m.RequiresAck && m.AcknowledgedAtUtc is null && !m.IsExpired(now)) : all;
        return new MessageList(open, shown.Select(m => MessageViews.Row(m, people, now)).ToList());
    }
}

public sealed record GetMessageQuery(Guid Id) : IQuery<MessageRow>;

public sealed class GetMessageQueryHandler(IMessageStore store, IStaffNames staff, TimeProvider clock) : IQueryHandler<GetMessageQuery, MessageRow>
{
    public async Task<Result<MessageRow>> Handle(GetMessageQuery query, CancellationToken cancellationToken)
    {
        var m = await store.GetAsync(query.Id, cancellationToken);
        if (m is null)
            return OperationsErrors.MessageNotFound(query.Id);

        return MessageViews.Row(m, await staff.GetNamesAsync([m.DriverSubject], cancellationToken), clock.GetUtcNow());
    }
}

public sealed record ListMessageTemplatesQuery : IQuery<IReadOnlyList<MessageTemplate>>;

public sealed class ListMessageTemplatesQueryHandler : IQueryHandler<ListMessageTemplatesQuery, IReadOnlyList<MessageTemplate>>
{
    public Task<Result<IReadOnlyList<MessageTemplate>>> Handle(ListMessageTemplatesQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(MessageTemplates.All));
}

// ---- controller: send and resend ----

// Address it to a driver, or to a journey (it goes to that journey's driver).
public sealed record SendMessageCommand(
    string? DriverSubject, Guid? JourneyId, Guid? IncidentId, string? Template, string Body, bool RequiresAck, DateTimeOffset ValidUntilUtc)
    : ICommand<MessageRow>;

public sealed class SendMessageCommandHandler(
    IMessageStore store, IOpsJourneyStats journeys, IStaffNames staff, IOperationsUnitOfWork unitOfWork, IOperationsAuditLog audit,
    ICurrentUser user, TimeProvider clock)
    : ICommandHandler<SendMessageCommand, MessageRow>
{
    public async Task<Result<MessageRow>> Handle(SendMessageCommand command, CancellationToken cancellationToken)
    {
        string? driver = command.DriverSubject;
        string? train = null;
        string? unit = null;

        if (command.JourneyId is { } journeyId)
        {
            var facts = await journeys.GetFactsAsync(journeyId, cancellationToken);
            if (facts is null)
                return Error.NotFound("Operations.JourneyNotFound", "That journey was not found.");

            driver ??= facts.DriverSubject;
            train = facts.TrainNumber;
            unit = facts.UnitCode;
        }

        if (string.IsNullOrWhiteSpace(driver))
            return OperationsErrors.RecipientRequired;

        var people = await staff.GetNamesAsync([driver], cancellationToken);
        if (!people.ContainsKey(driver))
            return OperationsErrors.RecipientRequired;

        var now = clock.GetUtcNow();
        var actor = user.UserId ?? "unknown";
        var message = ControlMessage.Send(driver, command.JourneyId, train, unit, command.IncidentId, command.Template, command.Body,
            command.RequiresAck, command.ValidUntilUtc, actor, now);
        if (message.IsFailure)
            return message.Error;

        store.Add(message.Value);
        audit.Record(new AuditRecord(actor, user.Roles.FirstOrDefault(), "message.send", "message", message.Value.Id.ToString(), null,
            After: new { Driver = driver, command.JourneyId, command.RequiresAck }));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MessageViews.Row(message.Value, people, now);
    }
}

public sealed record ResendMessageCommand(Guid Id, int ValidForMinutes) : ICommand<MessageRow>;

public sealed class ResendMessageCommandHandler(
    IMessageStore store, IStaffNames staff, IOperationsUnitOfWork unitOfWork, IOperationsAuditLog audit, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<ResendMessageCommand, MessageRow>
{
    public async Task<Result<MessageRow>> Handle(ResendMessageCommand command, CancellationToken cancellationToken)
    {
        var m = await store.GetAsync(command.Id, cancellationToken);
        if (m is null)
            return OperationsErrors.MessageNotFound(command.Id);

        var now = clock.GetUtcNow();
        var result = m.Resend(now, now.AddMinutes(Math.Clamp(command.ValidForMinutes <= 0 ? 15 : command.ValidForMinutes, 5, 240)));
        if (result.IsFailure)
            return result.Error;

        audit.Record(new AuditRecord(user.UserId ?? "unknown", user.Roles.FirstOrDefault(), "message.resend", "message", m.Id.ToString(), null));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MessageViews.Row(m, await staff.GetNamesAsync([m.DriverSubject], cancellationToken), now);
    }
}

// ---- driver: my messages and acknowledgement (scope: own) ----

public sealed record ListMyMessagesQuery : IQuery<IReadOnlyList<MessageRow>>;

public sealed class ListMyMessagesQueryHandler(
    IMessageStore store, IStaffNames staff, IOperationsUnitOfWork unitOfWork, ICurrentUser user, TimeProvider clock)
    : IQueryHandler<ListMyMessagesQuery, IReadOnlyList<MessageRow>>
{
    public async Task<Result<IReadOnlyList<MessageRow>>> Handle(ListMyMessagesQuery query, CancellationToken cancellationToken)
    {
        var me = user.UserId ?? string.Empty;
        var now = clock.GetUtcNow();
        var mine = await store.ListForDriverAsync(me, now.AddDays(-1), cancellationToken);

        // Fetching the list is what "Delivered" means: the driver's app has them.
        foreach (var m in mine.Where(m => m.DeliveredAtUtc is null && !m.IsExpired(now)))
            m.MarkDelivered(now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var people = await staff.GetNamesAsync([me], cancellationToken);
        return Result.Success<IReadOnlyList<MessageRow>>(mine.Select(m => MessageViews.Row(m, people, now)).ToList());
    }
}

public sealed record AcknowledgeMessageCommand(Guid Id) : ICommand;

public sealed class AcknowledgeMessageCommandHandler(
    IMessageStore store, IOperationsUnitOfWork unitOfWork, IOperationsAuditLog audit, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<AcknowledgeMessageCommand>
{
    public async Task<Result> Handle(AcknowledgeMessageCommand command, CancellationToken cancellationToken)
    {
        var m = await store.GetAsync(command.Id, cancellationToken);

        // Someone else's message looks like one that does not exist (no way to probe other drivers' messages).
        if (m is null || !string.Equals(m.DriverSubject, user.UserId, StringComparison.Ordinal))
            return OperationsErrors.MessageNotFound(command.Id);

        var result = m.Acknowledge(clock.GetUtcNow());
        if (result.IsFailure)
            return result;

        audit.Record(new AuditRecord(user.UserId ?? "unknown", user.Roles.FirstOrDefault(), "message.acknowledge", "message", m.Id.ToString(), null));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ---- assistance (a driver asks for help; the controller handles it) ----

public sealed record AssistanceRow(
    Guid Id,
    string DriverSubject,
    string? DriverName,
    string? TrainNumber,
    string? StationCode,
    string? StationName,
    string? Note,
    DateTimeOffset RequestedAtUtc,
    string Status,
    string? HandledBy,
    string? Outcome);

public sealed record RequestAssistanceCommand(Guid? JourneyId, string? StationCode, string? Note) : ICommand<AssistanceRow>;

public sealed class RequestAssistanceCommandHandler(
    IAssistanceStore store, IOpsJourneyStats journeys, IStaffNames staff, INetworkStations stations, IOperationsUnitOfWork unitOfWork,
    IOperationsAuditLog audit, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<RequestAssistanceCommand, AssistanceRow>
{
    public async Task<Result<AssistanceRow>> Handle(RequestAssistanceCommand command, CancellationToken cancellationToken)
    {
        var me = user.UserId ?? "unknown";
        string? train = null;
        var station = command.StationCode;

        if (command.JourneyId is { } id)
        {
            var facts = await journeys.GetFactsAsync(id, cancellationToken);
            if (facts is null)
                return Error.NotFound("Operations.JourneyNotFound", "That journey was not found.");

            // A driver asks for help only on their own journey.
            if (!string.Equals(facts.DriverSubject, me, StringComparison.Ordinal))
                return Error.Forbidden("Operations.NotYourJourney", "You can ask for assistance only on your own journey.");

            train = facts.TrainNumber;
            station ??= facts.NextStopCode ?? facts.StationCodes.FirstOrDefault();
        }

        var now = clock.GetUtcNow();
        var request = AssistanceRequest.Create(me, command.JourneyId, train, station, command.Note, now);
        store.Add(request);
        audit.Record(new AuditRecord(me, user.Roles.FirstOrDefault(), "assistance.request", "assistance", request.Id.ToString(), command.Note));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var names = await staff.GetNamesAsync([me], cancellationToken);
        var place = request.StationCode is null ? null : (await stations.GetNamesAsync([request.StationCode], null, cancellationToken)).GetValueOrDefault(request.StationCode);
        return Row(request, names, place);
    }

    internal static AssistanceRow Row(AssistanceRequest r, IReadOnlyDictionary<string, string> names, string? stationName) =>
        new(r.Id, r.DriverSubject, names.GetValueOrDefault(r.DriverSubject), r.TrainNumber, r.StationCode, stationName, r.Note, r.RequestedAtUtc,
            r.Status.ToString(), r.HandledBy, r.Outcome);
}

public sealed record ListAssistanceQuery : IQuery<IReadOnlyList<AssistanceRow>>;

public sealed class ListAssistanceQueryHandler(IAssistanceStore store, IStaffNames staff, INetworkStations stations)
    : IQueryHandler<ListAssistanceQuery, IReadOnlyList<AssistanceRow>>
{
    public async Task<Result<IReadOnlyList<AssistanceRow>>> Handle(ListAssistanceQuery query, CancellationToken cancellationToken)
    {
        var open = await store.ListOpenAsync(cancellationToken);
        var people = await staff.GetNamesAsync(open.Select(r => r.DriverSubject).Distinct(), cancellationToken);
        var places = await stations.GetNamesAsync(open.Select(r => r.StationCode).Where(c => c is not null).Select(c => c!).Distinct(), null, cancellationToken);
        return Result.Success<IReadOnlyList<AssistanceRow>>(open.Select(r => RequestAssistanceCommandHandler.Row(r, people, r.StationCode is null ? null : places.GetValueOrDefault(r.StationCode))).ToList());
    }
}

public sealed record HandleAssistanceCommand(Guid Id, string? Outcome) : ICommand;

public sealed class HandleAssistanceCommandHandler(IAssistanceStore store, IOperationsUnitOfWork unitOfWork, IOperationsAuditLog audit, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<HandleAssistanceCommand>
{
    public async Task<Result> Handle(HandleAssistanceCommand command, CancellationToken cancellationToken)
    {
        var r = await store.GetAsync(command.Id, cancellationToken);
        if (r is null)
            return OperationsErrors.AssistanceNotFound(command.Id);

        var actor = user.UserId ?? "unknown";
        var result = r.Handle(actor, command.Outcome, clock.GetUtcNow());
        if (result.IsFailure)
            return result;

        audit.Record(new AuditRecord(actor, user.Roles.FirstOrDefault(), "assistance.handle", "assistance", r.Id.ToString(), command.Outcome));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
