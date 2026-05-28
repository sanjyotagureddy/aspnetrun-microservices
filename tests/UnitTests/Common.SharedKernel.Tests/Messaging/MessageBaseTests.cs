using Common.SharedKernel.Messaging;

namespace Common.SharedKernel.Tests.Messaging;

public sealed class MessageBaseTests
{
    [Fact]
    public void MessageBase_ShouldCaptureIdentityAndTimestamp()
    {
        var before = DateTime.UtcNow;

        var message = new TestMessage();

        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, message.MessageId);
        Assert.InRange(message.OccurredOnUtc, before, after);
    }

    private sealed record TestMessage : MessageBase;
}