using Common.SharedKernel.Abstractions.Entities;

namespace Common.SharedKernel.Abstractions.Auditing;

public abstract class AuditableEntity<TId>(TId id) : Entity<TId>(id)
    where TId : notnull
{
    public DateTime? CreatedOnUtc { get; protected set; }

    public string? CreatedBy { get; protected set; }

    public DateTime? UpdatedOnUtc { get; protected set; }

    public string? UpdatedBy { get; protected set; }

    public void SetCreatedAudit(DateTime createdOnUtc, string? createdBy = null)
    {
        CreatedOnUtc = createdOnUtc;
        CreatedBy = createdBy;
        UpdatedOnUtc = createdOnUtc;
        UpdatedBy = createdBy;
    }

    public void SetUpdatedAudit(DateTime updatedOnUtc, string? updatedBy = null)
    {
        UpdatedOnUtc = updatedOnUtc;
        UpdatedBy = updatedBy;
    }
}