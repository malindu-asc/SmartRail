using SmartRail.Application.Core.Messaging;

namespace SmartRail.Modules.Journeys.Application.Generation;

// Creates the journeys for the service dates FromDate to FromDate + Days - 1 that do not exist yet (REQ-LIVE-008).
public sealed record GenerateJourneysCommand(DateOnly FromDate, int Days) : ICommand<GenerationResult>;

// DatesWithoutServices are dates where the timetable returned nothing (no version in force, or nothing runs).
public sealed record GenerationResult(int Created, int Skipped, int Failed, IReadOnlyList<DateOnly> DatesWithoutServices);
