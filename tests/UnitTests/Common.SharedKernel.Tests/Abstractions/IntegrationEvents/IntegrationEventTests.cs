using Common.SharedKernel.Abstractions.IntegrationEvents;

namespace Common.SharedKernel.Tests.Abstractions.IntegrationEvents;

public sealed class IntegrationEventTests
{
    [Fact]
    public void IntegrationEvent_ShouldCaptureIdAndOccurrenceTime()
    {
        var before = DateTime.UtcNow;

        var integrationEvent = new TestIntegrationEvent();

        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, integrationEvent.EventId);
        Assert.InRange(integrationEvent.OccurredOnUtc, before, after);
    }

    private sealed record TestIntegrationEvent : IntegrationEventBase
    {
        public override string EventType => "test.integration";
    }
}