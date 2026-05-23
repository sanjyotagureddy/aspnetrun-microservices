using Basket.API.Infrastructure.Messaging;
using MassTransit;
using Moq;
using Shared.Messaging.Events;
using Xunit;

namespace Basket.API.Test.Infrastructure;

public sealed class BasketCheckoutPublisherTests
{
    [Fact]
    public async Task PublishAsync_ForwardsEventToBus()
    {
        var publishEndpoint = new Mock<IPublishEndpoint>();
        var publisher = new BasketCheckoutPublisher(publishEndpoint.Object);
        var eventMessage = new BasketCheckoutEvent { UserName = "alice", TotalPrice = 42m };

        publishEndpoint.Setup(endpoint => endpoint.Publish(eventMessage, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await publisher.PublishAsync(eventMessage, CancellationToken.None);

        publishEndpoint.VerifyAll();
    }
}