namespace SharedKernel.Domain.Entities;

public abstract class Entity<TId>
    where TId : notnull
{

    public virtual TId Id { get; init; } = default!;

    public override bool Equals(object obj)
    {
        return obj is Entity<TId> other && GetType() == other.GetType() && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }
}
