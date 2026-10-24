using System.Text.RegularExpressions;
using FluentValidation;
using SmartRail.Application.Core.Messaging;

namespace SmartRail.Modules.Journeys.Application.Manual;

// A special or relief journey that is not in the timetable (REQ-LIVE-008). Times are HH:mm after the service date's midnight
// and may pass 24:00 (for example 24:25).
public sealed record CreateManualJourneyCommand(
    string RouteCode,
    string TrainNumber,
    DateOnly ServiceDate,
    IReadOnlyList<ManualStop> Stops,
    string Reason) : ICommand<Guid>;

public sealed record ManualStop(string StationCode, string? Arrival, string? Departure, string? Platform);

public sealed class CreateManualJourneyCommandValidator : AbstractValidator<CreateManualJourneyCommand>
{
    public CreateManualJourneyCommandValidator()
    {
        RuleFor(x => x.RouteCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.TrainNumber).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Stops).NotNull().Must(s => s is { Count: >= 2 }).WithMessage("A journey needs at least two stops.");
        RuleForEach(x => x.Stops).ChildRules(stop =>
        {
            stop.RuleFor(s => s.StationCode).NotEmpty().MaximumLength(5);
            stop.RuleFor(s => s.Arrival).Must(IsTimeOrEmpty).WithMessage("Times are written HH:mm.");
            stop.RuleFor(s => s.Departure).Must(IsTimeOrEmpty).WithMessage("Times are written HH:mm.");
            stop.RuleFor(s => s.Platform).MaximumLength(3);
        });
    }

    private static bool IsTimeOrEmpty(string? value) => string.IsNullOrWhiteSpace(value) || ManualTime.TryParse(value, out _);
}

// HH:mm after the service date's midnight; hours up to 47 so a stop can pass 24:00.
internal static partial class ManualTime
{
    internal static bool TryParse(string? value, out int minutes)
    {
        minutes = 0;
        var match = TimePattern().Match(value?.Trim() ?? string.Empty);
        if (!match.Success)
            return false;

        var hours = int.Parse(match.Groups[1].Value);
        var mins = int.Parse(match.Groups[2].Value);
        if (hours > 47 || mins > 59)
            return false;

        minutes = hours * 60 + mins;
        return true;
    }

    [GeneratedRegex(@"^(\d{1,2}):(\d{2})$")]
    private static partial Regex TimePattern();
}
