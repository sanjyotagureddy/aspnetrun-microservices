using MediatR;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using Ordering.Application.Features.Orders.Commands.CheckoutOrder;
using Ordering.Application.Features.Orders.Commands.DeleteCommand;
using Ordering.Application.Features.Orders.Commands.UpdateOrder;
using Ordering.Application.Features.Orders.Queries.GetOrdersList;

using SharedKernel.Web;
using SharedKernel.Errors;

namespace Ordering.API.Endpoints;

internal sealed class OrderingEndpoints : IEndpoint
{
    private const string IdempotencyHeaderName = "Idempotency-Key";
    private const string IdempotencyErrorCode = "ordering.idempotency_key_required";

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder orders = app.MapGroup("api/v1/order")
            .WithTags("Order");

        orders.MapGet("{userName}", GetOrdersByUserName)
            .WithName("GetOrder");

        orders.MapPost(string.Empty, CheckoutOrder)
            .WithName("CheckoutOrder");

        orders.MapPut(string.Empty, UpdateOrder)
            .WithName("UpdateOrder");

        orders.MapDelete("{id:int}", DeleteOrder)
            .WithName("DeleteOrder");
    }

    internal static async Task<Ok<IEnumerable<OrdersVm>>> GetOrdersByUserName(
        [FromServices] IMediator mediator,
        [FromRoute] string userName,
        CancellationToken cancellationToken)
    {
        List<OrdersVm> orders = await mediator.Send(new GetOrdersListQuery(userName), cancellationToken);
        return TypedResults.Ok(orders.AsEnumerable());
    }

    internal static async Task<Ok<int>> CheckoutOrder(
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        [FromBody] CheckoutOrderCommand command,
        CancellationToken cancellationToken)
    {
        if (!httpContext.Request.Headers.TryGetValue(IdempotencyHeaderName, out var key) || string.IsNullOrWhiteSpace(key))
        {
            throw Errors.Common.Validation(
                "Missing idempotency key",
                new[] { new Info(IdempotencyErrorCode, $"Header '{IdempotencyHeaderName}' is required for checkout requests.") });
        }

        int orderId = await mediator.Send(command, cancellationToken);
        return TypedResults.Ok(orderId);
    }

    internal static async Task<NoContent> UpdateOrder(
        [FromServices] IMediator mediator,
        [FromBody] UpdateOrderCommand command,
        CancellationToken cancellationToken)
    {
        await mediator.Send(command, cancellationToken);
        return TypedResults.NoContent();
    }

    internal static async Task<NoContent> DeleteOrder(
        [FromServices] IMediator mediator,
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteOrderCommand { Id = id }, cancellationToken);
        return TypedResults.NoContent();
    }
}