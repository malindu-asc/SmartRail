namespace SmartRail.Modules.Network.Domain.Timetables;

public sealed record TimetableVersionSummary(Guid Id, TimetableVersionStatus Status, DateOnly? EffectiveFrom);

public static class VersionResolver
{
    // The version in force on a date: Published or Superseded, with the greatest effective date on or before it.
    // A Superseded version still answers for the dates before its successor takes over (REQ-LIVE-017).
    public static TimetableVersionSummary? Resolve(IEnumerable<TimetableVersionSummary> versions, DateOnly date) =>
        versions
            .Where(v => v.Status is TimetableVersionStatus.Published or TimetableVersionStatus.Superseded
                        && v.EffectiveFrom is not null
                        && v.EffectiveFrom <= date)
            .OrderByDescending(v => v.EffectiveFrom)
            .FirstOrDefault();
}
