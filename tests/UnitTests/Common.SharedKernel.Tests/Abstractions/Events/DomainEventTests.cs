using Common.SharedKernel.Abstractions.Events;

namespace Common.SharedKernel.Tests.Abstractions.Events;

public sealed class DomainEventTests
{
    [Fact]
    public void DomainEvent_ShouldCaptureOccurrenceTime()
    {
        var before = DateTime.UtcNow;

        var domainEvent = new TestDomainEvent();

        var after = DateTime.UtcNow;

        Assert.InRange(domainEvent.OccurredOnUtc, before, after);
        Assert.Equal(nameof(TestDomainEvent), domainEvent.EventType);
    }

    private sealed record TestDomainEvent : DomainEvent;
}