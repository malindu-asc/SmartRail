using FluentValidation;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Contracts;
using SmartRail.Modules.Network.Contracts;
using SmartRail.Modules.Operations.Application.Abstractions;
using SmartRail.Modules.Operations.Domain.Disruptions;
using SmartRail.Modules.Operations.Domain.Errors;
using SmartRail.SharedKernel.Abstractions;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Operations.Application.Disruptions;

public interface IDisruptionStore
{
    Task<Disruption?> GetAsync(Guid id, CancellationToken cancellationToken);

    // Published disruptions, newest first (the ones passengers see).
    Task<IReadOnlyList<Disruption>> ListPublishedAsync(CancellationToken cancellationToken);

    // Everything that is not closed, for the console: drafts, published and expired.
    Task<IReadOnlyList<Disruption>> ListOpenAsync(CancellationToken cancellationToken);

    void Add(Disruption disruption);
}

// ---- What passengers see ----

public sealed record DisruptionSummary(
    Guid Id,
    string Title,
    string Message,
    string Severity,
    bool NetworkWide,
    IReadOnlyList<DisruptionStation> Stations,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ExpectedEndUtc);

public sealed record DisruptionStation(string Code, string Name);

public sealed record DisruptionDetail(DisruptionSummary Disruption, IReadOnlyList<DisruptionJourney> Journeys);

public sealed record DisruptionJourney(
    Guid JourneyId,
    string TrainNumber,
    string StationCode,
    string DestinationName,
    DateTimeOffset ScheduledDepartureUtc,
    DateTimeOffset ExpectedDepartureUtc,
    string Status,
    int? DelayMinutes,
    string? Platform);

internal static class DisruptionViews
{
    public static async Task<DisruptionSummary> SummaryAsync(Disruption d, INetworkStations stations, string? language, CancellationToken cancellationToken)
    {
        var names = await stations.GetNamesAsync(d.StationCodes, language, cancellationToken);
        return new DisruptionSummary(
            d.Id, d.Title, d.Message, d.Severity.ToString(), d.NetworkWide,
            d.StationCodes.Select(c => new DisruptionStation(c, names.GetValueOrDefault(c, c))).ToList(),
            d.UpdatedAtUtc, d.ExpectedEndUtc);
    }
}

public sealed record ListDisruptionsQuery(string? Language) : IQuery<IReadOnlyList<DisruptionSummary>>;

public sealed class ListDisruptionsQueryHandler(IDisruptionStore store, INetworkStations stations)
    : IQueryHandler<ListDisruptionsQuery, IReadOnlyList<DisruptionSummary>>
{
    public async Task<Result<IReadOnlyList<DisruptionSummary>>> Handle(ListDisruptionsQuery query, CancellationToken cancellationToken)
    {
        var list = new List<DisruptionSummary>();
        foreach (var d in await store.ListPublishedAsync(cancellationToken))
            list.Add(await DisruptionViews.SummaryAsync(d, stations, query.Language, cancellationToken));

        return Result.Success<IReadOnlyList<DisruptionSummary>>(list);
    }
}

public sealed record GetDisruptionQuery(Guid Id, string? Language) : IQuery<DisruptionDetail>;

public sealed class GetDisruptionQueryHandler(IDisruptionStore store, INetworkStations stations, IAffectedJourneys affected)
    : IQueryHandler<GetDisruptionQuery, DisruptionDetail>
{
    public async Task<Result<DisruptionDetail>> Handle(GetDisruptionQuery query, CancellationToken cancellationToken)
    {
        var d = await store.GetAsync(query.Id, cancellationToken);

        // A draft, a resolved or a retracted disruption is not public: it looks the same as one that never existed.
        if (d is null || d.Status != DisruptionStatus.Published)
            return OperationsErrors.NotFound(query.Id);

        var journeys = d.NetworkWide || d.StationCodes.Count == 0
            ? []
            : await affected.FindAsync(d.StationCodes, 10, query.Language, cancellationToken);

        return new DisruptionDetail(
            await DisruptionViews.SummaryAsync(d, stations, query.Language, cancellationToken),
            journeys.Select(j => new DisruptionJourney(
                j.JourneyId, j.TrainNumber, j.StationCode, j.DestinationName, j.ScheduledDepartureUtc,
                j.ExpectedDepartureUtc, j.Status, j.DelayMinutes, j.Platform)).ToList());
    }
}

// ---- What controllers do ----

public sealed record CreateDisruptionCommand(
    string Actor, string Title, string Message, string Severity, bool NetworkWide, IReadOnlyList<string> StationCodes,
    DateTimeOffset? ExpectedEndUtc, DateTimeOffset ReviewByUtc) : ICommand<Guid>;

public sealed class CreateDisruptionCommandValidator : AbstractValidator<CreateDisruptionCommand>
{
    public CreateDisruptionCommandValidator()
    {
        RuleFor(x => x.Severity).Must(s => Enum.TryParse<DisruptionSeverity>(s, true, out _)).WithMessage("Severity is Minor, Major or Severe.");
    }
}

public sealed class CreateDisruptionCommandHandler(IDisruptionStore store, IOperationsUnitOfWork unitOfWork, TimeProvider clock)
    : ICommandHandler<CreateDisruptionCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateDisruptionCommand command, CancellationToken cancellationToken)
    {
        var created = Disruption.Create(
            command.Title, command.Message, Enum.Parse<DisruptionSeverity>(command.Severity, true), command.NetworkWide,
            command.StationCodes, command.ExpectedEndUtc, command.ReviewByUtc, command.Actor, clock.GetUtcNow());
        if (created.IsFailure)
            return created.Error;

        store.Add(created.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return created.Value.Id;
    }
}

public enum DisruptionAction { Approve, Resolve, Retract }

public sealed record ActOnDisruptionCommand(Guid Id, string Actor, DisruptionAction Action) : ICommand;

public sealed class ActOnDisruptionCommandHandler(IDisruptionStore store, IOperationsUnitOfWork unitOfWork, TimeProvider clock)
    : ICommandHandler<ActOnDisruptionCommand>
{
    public async Task<Result> Handle(ActOnDisruptionCommand command, CancellationToken cancellationToken)
    {
        var d = await store.GetAsync(command.Id, cancellationToken);
        if (d is null)
            return OperationsErrors.NotFound(command.Id);

        var now = clock.GetUtcNow();
        var result = command.Action switch
        {
            DisruptionAction.Approve => d.Approve(command.Actor, now),
            DisruptionAction.Resolve => d.Resolve(now),
            _ => d.Retract(now)
        };
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
