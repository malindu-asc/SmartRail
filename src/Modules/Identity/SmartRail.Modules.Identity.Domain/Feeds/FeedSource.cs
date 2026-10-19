using SmartRail.SharedKernel.Primitives;

namespace SmartRail.Modules.Identity.Domain.Feeds;

// One authenticated sender of live data. Only the hash of its key is stored (REQ-SEC-004).
public sealed class FeedSource : Entity<Guid>
{
    private FeedSource() { }

    public FeedSource(Guid id, string code, string name, string keyHash, int precedenceRank, DateTimeOffset now)
        : base(id)
    {
        Code = code;
        Name = name;
        KeyHash = keyHash;
        PrecedenceRank = precedenceRank;
        IsActive = true;
        CreatedAt = now;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public int PrecedenceRank { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RotatedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }

    public void RecordUse(DateTimeOffset now) => LastUsedAt = now;

    public void RotateKey(string keyHash, DateTimeOffset now)
    {
        KeyHash = keyHash;
        RotatedAt = now;
    }

    public void Deactivate() => IsActive = false;
}
