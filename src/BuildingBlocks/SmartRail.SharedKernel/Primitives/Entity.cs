namespace SmartRail.SharedKernel.Primitives;

public abstract class Entity<TKey> : IEquatable<Entity<TKey>>, IHasDomainEvents
    where TKey : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity(TKey id) => Id = id;

    protected Entity() { }

    public TKey Id { get; protected set; } = default!;

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public bool Equals(Entity<TKey>? other) =>
        other is not null && (ReferenceEquals(this, other) || EqualityComparer<TKey>.Default.Equals(Id, other.Id));

    public override bool Equals(object? obj) => obj is Entity<TKey> entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right) => Equals(left, right);

    public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right) => !Equals(left, right);
}

public interface IAuditableEntity
{
    DateTimeOffset CreatedAtUtc { get; set; }
    DateTimeOffset? UpdatedAtUtc { get; set; }
}

public abstract class AuditableEntity<TKey> : Entity<TKey>, IAuditableEntity
    where TKey : notnull
{
    protected AuditableEntity(TKey id) : base(id) { }

    protected AuditableEntity() { }

    // Set by AuditableEntitiesInterceptor, never by domain code.
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}

public abstract class AuditableEntity : AuditableEntity<Guid>
{
    protected AuditableEntity(Guid id) : base(id) { }

    protected AuditableEntity() { }
}
