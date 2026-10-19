using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Identity.Domain.Feeds;
using SmartRail.Modules.Identity.Infrastructure.Persistence;

namespace SmartRail.Modules.Identity.Infrastructure.Feeds;

public static class FeedKeyHasher
{
    public static string Hash(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
}

internal sealed class FeedSources(IdentityDbContext context, TimeProvider clock) : IFeedSources
{
    // A source that is used all the time must not cause a write on every request.
    private static readonly TimeSpan LastUsedGranularity = TimeSpan.FromMinutes(5);

    // Hashed even when the code is unknown, so timing does not reveal which codes exist.
    private static readonly byte[] DummyHash = SHA256.HashData(Encoding.UTF8.GetBytes("no-such-source"));

    public async Task<FeedSourceRef?> AuthenticateAsync(string code, string key, CancellationToken cancellationToken)
    {
        var source = await context.FeedSources.FirstOrDefaultAsync(s => s.Code == code, cancellationToken);

        var presented = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var stored = source is null ? DummyHash : Convert.FromHexString(source.KeyHash);

        var matches = CryptographicOperations.FixedTimeEquals(presented, stored);
        if (source is null || !matches || !source.IsActive)
            return null;

        var now = clock.GetUtcNow();
        if (source.LastUsedAt is null || now - source.LastUsedAt >= LastUsedGranularity)
        {
            source.RecordUse(now);
            await context.SaveChangesAsync(cancellationToken);
        }

        return new FeedSourceRef(source.Id, source.Code, source.PrecedenceRank);
    }
}

// Development and test sources from configuration (Identity:FeedSources:Seed). Insert-only: an existing source is never changed.
public sealed class FeedSourceSeeder(
    IdentityDbContext context,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<FeedSourceSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var wanted = configuration.GetSection("Identity:FeedSources:Seed").Get<List<FeedSourceSeed>>() ?? [];
        var existing = await context.FeedSources.Select(s => s.Code).ToListAsync(cancellationToken);

        var added = 0;
        foreach (var seed in wanted.Where(s => !existing.Contains(s.Code)))
        {
            if (string.IsNullOrWhiteSpace(seed.Code) || string.IsNullOrWhiteSpace(seed.Key) || seed.Rank < 1)
                continue;

            context.FeedSources.Add(new FeedSource(
                DeterministicId(seed.Code), seed.Code, seed.Name ?? seed.Code, FeedKeyHasher.Hash(seed.Key), seed.Rank, clock.GetUtcNow()));
            added++;
        }

        if (added > 0)
            await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Feed source seed: {Added} added", added);
    }

    private static Guid DeterministicId(string code) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes($"smartrail:identity:feed-source:{code}")));

    private sealed class FeedSourceSeed
    {
        public string Code { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string Key { get; set; } = string.Empty;
        public int Rank { get; set; }
    }
}
