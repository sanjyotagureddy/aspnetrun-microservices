namespace Common.SharedKernel.Messaging.UnitTests.Exceptions;

public sealed class MessagingExceptionsTests
{
    [Fact]
    public void MessagingException_ShouldCaptureMessageAndInnerException()
    {
        InvalidOperationException inner = new("inner");

        MessagingException exception = new("boom", inner);

        exception.Message.Should().Be("boom");
        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void MessagingConfigurationException_ShouldDeriveFromMessagingException()
    {
        MessagingConfigurationException exception = new("invalid-config");

        exception.Should().BeOfType<MessagingConfigurationException>();
        exception.Should().BeAssignableTo<MessagingException>();
        exception.Message.Should().Be("invalid-config");
    }
}
