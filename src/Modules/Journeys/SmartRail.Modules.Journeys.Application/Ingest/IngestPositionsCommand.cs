using FluentValidation;
using SmartRail.Application.Core.Messaging;

namespace SmartRail.Modules.Journeys.Application.Ingest;

// Positions from the train detection feed: which section a vehicle is in and how far along it is (REQ-LIVE-005, 010, 015).
public sealed record IngestPositionsCommand(FeedSender Source, IReadOnlyList<PositionInput> Readings) : ICommand<IngestResponse>;

public sealed record PositionInput(
    long Sequence,
    string VehicleCode,
    DateTimeOffset RecordedAt,
    string FromStation,
    string ToStation,
    decimal Progress,
    decimal SpeedKmh);

public sealed class IngestPositionsCommandValidator : AbstractValidator<IngestPositionsCommand>
{
    public IngestPositionsCommandValidator()
    {
        RuleFor(x => x.Readings)
            .NotNull()
            .Must(r => r is { Count: >= 1 and <= IngestLimits.MaxBatch })
            .WithMessage($"A batch has 1 to {IngestLimits.MaxBatch} readings.");

        RuleForEach(x => x.Readings).ChildRules(r =>
        {
            r.RuleFor(i => i.Sequence).GreaterThan(0);
            r.RuleFor(i => i.VehicleCode).NotEmpty().MaximumLength(30);
            r.RuleFor(i => i.FromStation).NotEmpty().MaximumLength(5);
            r.RuleFor(i => i.ToStation).NotEmpty().MaximumLength(5);
            r.RuleFor(i => i.Progress).InclusiveBetween(0m, 1m);
            r.RuleFor(i => i.SpeedKmh).GreaterThanOrEqualTo(0m).LessThan(1000m);
        });
    }
}
