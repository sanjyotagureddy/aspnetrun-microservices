namespace SharedKernel.Domain.Entities;

public abstract class SoftDeletableEntity<TId> : AuditableEntity<TId>
    where TId : notnull
{
    public bool IsDeleted { get; protected set; }

    public DateTimeOffset? DeletedAtUtc { get; protected set; }

    public string DeletedBy { get; protected set; }

    public void MarkDeleted(string deletedBy)
    {
        IsDeleted = true;
        DeletedBy = deletedBy ?? string.Empty;
        DeletedAtUtc = DateTimeOffset.UtcNow;
        SetLastModified(deletedBy);
    }

    public void Restore(string restoredBy)
    {
        IsDeleted = false;
        DeletedBy = string.Empty;
        DeletedAtUtc = null;
        SetLastModified(restoredBy);
    }
}
