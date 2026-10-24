namespace SmartRail.Modules.Identity.Contracts;

// A machine that sends live data (the train detection feed, the simulator). Rank 1 is the most trusted source
// (docs/00-requirements/04-domain/source-precedence.md).
public sealed record FeedSourceRef(Guid Id, string Code, int Rank);

public interface IFeedSources
{
    // Null when the code is unknown, the key is wrong or the source is switched off. Callers never learn which.
    Task<FeedSourceRef?> AuthenticateAsync(string code, string key, CancellationToken cancellationToken);
}
