using MediatR;
using SmartRail.Modules.Journeys.Contracts;

namespace SmartRail.Modules.Journeys.Application.Queries.GetBoard;

// Answers the contract from the same boards the passenger app shows, so a disruption and a board never disagree.
public sealed class AffectedJourneys(ISender sender) : IAffectedJourneys
{
    public async Task<IReadOnlyList<AffectedJourney>> FindAsync(
        IReadOnlyCollection<string> stationCodes, int count, string? language, CancellationToken cancellationToken)
    {
        var found = new Dictionary<Guid, AffectedJourney>();

        foreach (var code in stationCodes.Select(c => c.Trim().ToUpperInvariant()).Distinct())
        {
            var board = await sender.Send(new GetBoardQuery(code, 10, language), cancellationToken);
            if (board.IsFailure)
                continue;

            foreach (var item in board.Value.Items)
            {
                found.TryAdd(item.JourneyId, new AffectedJourney(
                    item.JourneyId, item.TrainNumber, code, item.DestinationCode, item.DestinationName,
                    item.ScheduledDepartureUtc, item.ExpectedDepartureUtc, item.Status, item.DelayMinutes, item.Platform));
            }
        }

        return found.Values.OrderBy(j => j.ExpectedDepartureUtc).Take(count).ToList();
    }
}
