using Common.SharedKernel.Abstractions.Entities;
using Common.SharedKernel.Abstractions.Events;

namespace Common.SharedKernel.Tests.Abstractions.Entities;

public sealed class EntityTests
{
    [Fact]
    public void Entities_WithSameTypeAndId_ShouldBeEqual()
    {
        var first = new TestEntity(1);
        var second = new TestEntity(1);

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void Entity_ShouldTrackDomainEvents()
    {
        var entity = new TestEntity(1);
        var domainEvent = new TestDomainEvent();

        entity.AddDomainEvent(domainEvent);

        Assert.Single(entity.DomainEvents);
        Assert.Contains(domainEvent, entity.DomainEvents);

        entity.ClearDomainEvents();

        Assert.Empty(entity.DomainEvents);
    }

    private sealed class TestEntity(int id) : Entity<int>(id);

    private sealed record TestDomainEvent : DomainEvent;
}
