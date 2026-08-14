using Cart.Api.Events;
using Common.SharedKernel.Messaging;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Order.Api.Data;
using Order.Api.Domain;
using Order.Api.Events;
using Order.Api.Features.Orders.CancelOrder;
using Order.Api.Features.Orders.CreateOrder;
using Order.Api.Features.Orders.GetOrderById;
using Order.Api.Features.Orders.GetOrdersByUserName;
using Order.Api.Messaging;
using Xunit;

namespace Order.Api.Tests;

public class OrderFeatureTests
{
    private readonly IOrderRepository _repository = Substitute.For<IOrderRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IMessageBus _messageBus = Substitute.For<IMessageBus>();
    private readonly ILogger<BasketCheckoutIntegrationEventHandler> _handlerLogger = Substitute.For<ILogger<BasketCheckoutIntegrationEventHandler>>();

    [Fact]
    public async Task CreateOrderHandler_ShouldCreateAndReturnOrder()
    {
        // Arrange
        var command = new CreateOrderCommand(
            "sanjyota",
            250.00m,
            "Sanjyota",
            "Gureddy",
            "sanjyota@example.com",
            "123 Main St",
            "USA",
            "CA",
            "90210",
            "Sanjyota Gureddy",
            "1111222233334444",
            "12/28",
            "123",
            1,
            new List<OrderItemDto> { new OrderItemDto("prod-1", "Gaming Mouse", 250.00m, 1) });

        var createdEntity = new OrderEntity
        {
            Id = Guid.NewGuid(),
            UserName = command.UserName,
            TotalPrice = command.TotalPrice,
            EmailAddress = command.EmailAddress
        };

        _repository.CreateOrderAsync(Arg.Any<OrderEntity>(), Arg.Any<CancellationToken>())
            .Returns(createdEntity);

        var handler = new CreateOrderHandler(_repository);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be("sanjyota");
        result.TotalPrice.Should().Be(250.00m);
        await _repository.Received(1).CreateOrderAsync(Arg.Any<OrderEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrdersHandler_ShouldReturnUserOrders()
    {
        // Arrange
        var expectedOrders = new List<OrderEntity>
        {
            new() { Id = Guid.NewGuid(), UserName = "sanjyota", TotalPrice = 100m },
            new() { Id = Guid.NewGuid(), UserName = "sanjyota", TotalPrice = 150m }
        };

        _repository.GetOrdersByUserNameAsync("sanjyota", Arg.Any<CancellationToken>())
            .Returns(expectedOrders);

        var handler = new GetOrdersHandler(_repository);

        // Act
        var result = await handler.Handle(new GetOrdersQuery("sanjyota"), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.First().UserName.Should().Be("sanjyota");
    }

    [Fact]
    public async Task GetOrderByIdHandler_ShouldReturnOrder_WhenFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var expectedOrder = new OrderEntity { Id = orderId, UserName = "sanjyota", TotalPrice = 99.99m };

        _repository.GetOrderByIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns(expectedOrder);

        var handler = new GetOrderByIdHandler(_repository);

        // Act
        var result = await handler.Handle(new GetOrderByIdQuery(orderId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
    }

    [Fact]
    public async Task CancelOrderHandler_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _repository.CancelOrderAsync(orderId, Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new CancelOrderHandler(_repository);

        // Act
        var result = await handler.Handle(new CancelOrderCommand(orderId), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task BasketCheckoutIntegrationEventHandler_ShouldProcessCheckoutAndPublishOrderCreatedEvent()
    {
        // Arrange
        var checkoutEvent = new BasketCheckoutIntegrationEvent(
            "sanjyota",
            199.99m,
            "Sanjyota",
            "Gureddy",
            "sanjyota@example.com",
            "123 Street",
            "USA",
            "NY",
            "10001",
            "Card Holder",
            "4111222233334444",
            "05/27",
            "999",
            1,
            DateTime.UtcNow);

        var createdOrder = new OrderEntity
        {
            Id = Guid.NewGuid(),
            UserName = "sanjyota",
            TotalPrice = 199.99m,
            Items = new List<OrderItem> { new OrderItem { ProductId = "p1", ProductName = "Item 1", UnitPrice = 199.99m, Quantity = 1 } }
        };

        _sender.Send(Arg.Any<CreateOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(createdOrder);

        var handler = new BasketCheckoutIntegrationEventHandler(_sender, _messageBus, _handlerLogger);

        var context = Substitute.For<IMessageContext>();

        // Act
        await handler.HandleAsync(checkoutEvent, context, CancellationToken.None);

        // Assert
        await _sender.Received(1).Send(Arg.Any<CreateOrderCommand>(), Arg.Any<CancellationToken>());
        await _messageBus.Received(1).PublishAsync(OrderCreatedIntegrationEvent.Topic, Arg.Any<OrderCreatedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }
}
