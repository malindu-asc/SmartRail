using FluentValidation;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Domain.Live;

namespace SmartRail.Modules.Journeys.Application.Ingest;

// Stop events from the train detection feed (REQ-LIVE-005, 006). Type is arrived, departed or passed.
public sealed record IngestStopEventsCommand(FeedSender Source, IReadOnlyList<StopEventInput> Events) : ICommand<IngestResponse>;

public sealed record StopEventInput(long Sequence, Guid JourneyId, int StopSequence, string Type, DateTimeOffset OccurredAt);

public sealed class IngestStopEventsCommandValidator : AbstractValidator<IngestStopEventsCommand>
{
    public IngestStopEventsCommandValidator()
    {
        RuleFor(x => x.Events)
            .NotNull()
            .Must(e => e is { Count: >= 1 and <= IngestLimits.MaxBatch })
            .WithMessage($"A batch has 1 to {IngestLimits.MaxBatch} events.");

        RuleForEach(x => x.Events).ChildRules(e =>
        {
            e.RuleFor(i => i.Sequence).GreaterThan(0);
            e.RuleFor(i => i.JourneyId).NotEmpty();
            e.RuleFor(i => i.StopSequence).GreaterThanOrEqualTo(1);
            e.RuleFor(i => i.Type).Must(t => TryParseType(t, out _)).WithMessage("The type is arrived, departed or passed.");
        });
    }

    internal static bool TryParseType(string? value, out StopEventType type) =>
        Enum.TryParse(value, ignoreCase: true, out type) && Enum.IsDefined(type);
}
