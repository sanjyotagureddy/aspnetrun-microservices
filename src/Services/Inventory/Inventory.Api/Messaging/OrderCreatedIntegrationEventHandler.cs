using Common.SharedKernel.Messaging;
using Microsoft.Extensions.Logging;
using Order.Api.Events;

namespace Inventory.Api.Messaging;

public class OrderCreatedIntegrationEventHandler : IMessageHandler<OrderCreatedIntegrationEvent>
{
    private readonly ILogger<OrderCreatedIntegrationEventHandler> _logger;

    public OrderCreatedIntegrationEventHandler(ILogger<OrderCreatedIntegrationEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task HandleAsync(
        OrderCreatedIntegrationEvent message,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing OrderCreatedIntegrationEvent for OrderId: {OrderId}, TotalItems: {Count}",
            message.OrderId, message.Items.Count);

        foreach (var item in message.Items)
        {
            _logger.LogInformation("Deducting inventory stock for ProductId: {ProductId}, Quantity: {Quantity}",
                item.ProductId, item.Quantity);
        }

        return Task.CompletedTask;
    }
}
