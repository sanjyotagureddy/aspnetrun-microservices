using AutoMapper;

using MassTransit;

using MediatR;

using Microsoft.Extensions.Logging;

using Moq;

using Ordering.API.EventBusConsumers;
using Ordering.Application.Features.Orders.Commands.CheckoutOrder;

using Shared.Messaging.Events;

using Xunit;

namespace Ordering.API.Test;

public sealed class BasketCheckoutConsumerTests
{
    [Fact]
    public async Task Consume_MapsEventAndSendsCommand()
    {
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BasketCheckoutConsumer>>();

        var message = new BasketCheckoutEvent { UserName = "swn", CardNumber = "1234" };
        var command = new CheckoutOrderCommand { UserName = "swn", CardNumber = "1234" };

        mapper.Setup(m => m.Map<CheckoutOrderCommand>(It.Is<BasketCheckoutEvent>(value => value == message)))
            .Returns(command);

        mediator.Setup(m => m.Send(It.Is<CheckoutOrderCommand>(value => value == command), It.IsAny<CancellationToken>()))
            .ReturnsAsync(9);

        var context = new Mock<ConsumeContext<BasketCheckoutEvent>>(MockBehavior.Strict);
        context.SetupGet(c => c.Message).Returns(message);

        var consumer = new BasketCheckoutConsumer(mapper.Object, mediator.Object, logger.Object);

        await consumer.Consume(context.Object);

        mapper.VerifyAll();
        mediator.VerifyAll();
    }
}