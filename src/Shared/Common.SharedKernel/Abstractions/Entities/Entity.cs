using Common.SharedKernel.Abstractions.Events;

namespace Common.SharedKernel.Abstractions.Entities;

public abstract class Entity<TId>(TId id) : IEquatable<Entity<TId>>
    where TId : notnull
{
    private readonly List<IDomainEvent> domainEvents = [];

    public TId Id { get; } = id;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        Common.SharedKernel.Helpers.Guard.Against.Null(domainEvent);

        domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => domainEvents.Clear();

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetType() == other.GetType() && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}