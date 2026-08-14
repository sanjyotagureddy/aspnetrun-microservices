using Cart.Api.Events;
using Common.SharedKernel.Messaging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Order.Api.Events;
using Order.Api.Features.Orders.CreateOrder;

namespace Order.Api.Messaging;

public class BasketCheckoutIntegrationEventHandler : IMessageHandler<BasketCheckoutIntegrationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BasketCheckoutIntegrationEventHandler> _logger;

    public BasketCheckoutIntegrationEventHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<BasketCheckoutIntegrationEventHandler> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(
        BasketCheckoutIntegrationEvent message,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing BasketCheckoutIntegrationEvent for User: {UserName}, TotalPrice: {TotalPrice}",
            message.UserName, message.TotalPrice);

        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var items = new List<OrderItemDto>();

        var command = new CreateOrderCommand(
            message.UserName,
            message.TotalPrice,
            message.FirstName,
            message.LastName,
            message.EmailAddress,
            message.AddressLine,
            message.Country,
            message.State,
            message.ZipCode,
            message.CardName,
            message.CardNumber,
            message.Expiration,
            message.CVV,
            message.PaymentMethod,
            items);

        var createdOrder = await sender.Send(command, cancellationToken);

        var orderCreatedEvent = new OrderCreatedIntegrationEvent(
            createdOrder.Id,
            createdOrder.UserName,
            createdOrder.TotalPrice,
            createdOrder.Items.Select(i => new OrderItemIntegrationDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList(),
            DateTime.UtcNow);

        await messageBus.PublishAsync(
            OrderCreatedIntegrationEvent.Topic,
            orderCreatedEvent,
            meta => meta.OrderingKey = createdOrder.Id.ToString(),
            cancellationToken);

        _logger.LogInformation("OrderCreatedIntegrationEvent published for OrderId: {OrderId}", createdOrder.Id);
    }
}
