using Common.SharedKernel.Abstractions;
using Common.SharedKernel.Abstractions.Auditing;

namespace Common.SharedKernel.Tests.Abstractions.Auditing;

public sealed class AuditableEntityTests
{
    [Fact]
    public void SetCreatedAudit_ShouldSetAuditMetadata()
    {
        var createdOnUtc = new DateTime(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc);
        var entity = new TestAuditableEntity(1);

        entity.SetCreatedAudit(createdOnUtc, "system");

        Assert.Equal(createdOnUtc, entity.CreatedOnUtc);
        Assert.Equal("system", entity.CreatedBy);
        Assert.Equal(createdOnUtc, entity.UpdatedOnUtc);
        Assert.Equal("system", entity.UpdatedBy);
    }

    [Fact]
    public void SetUpdatedAudit_ShouldUpdateLastModifiedMetadata()
    {
        var entity = new TestAuditableEntity(1);
        var modifiedOnUtc = new DateTime(2026, 5, 28, 13, 0, 0, DateTimeKind.Utc);

        entity.SetUpdatedAudit(modifiedOnUtc, "editor");

        Assert.Equal(modifiedOnUtc, entity.UpdatedOnUtc);
        Assert.Equal("editor", entity.UpdatedBy);
    }

    private sealed class TestAuditableEntity(int id) : AuditableEntity<int>(id);
}