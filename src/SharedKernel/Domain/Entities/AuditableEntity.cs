namespace SharedKernel.Domain.Entities;

public abstract class AuditableEntity<TId> : Entity<TId>
    where TId : notnull
{
    public DateTimeOffset CreatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;

    public string CreatedBy { get; protected set; } = string.Empty;

    public DateTimeOffset? LastModifiedAtUtc { get; protected set; }

    public string LastModifiedBy { get; protected set; }

    public void SetCreated(string createdBy)
    {
        CreatedBy = createdBy ?? string.Empty;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetLastModified(string modifiedBy)
    {
        LastModifiedBy = modifiedBy ?? string.Empty;
        LastModifiedAtUtc = DateTimeOffset.UtcNow;
    }
}
