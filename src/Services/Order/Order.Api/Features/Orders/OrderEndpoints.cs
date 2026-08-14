using MediatR;
using Microsoft.AspNetCore.Mvc;
using Order.Api.Domain;
using Order.Api.Features.Orders.CancelOrder;
using Order.Api.Features.Orders.CreateOrder;
using Order.Api.Features.Orders.GetOrderById;
using Order.Api.Features.Orders.GetOrdersByUserName;

namespace Order.Api.Features.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders")
            .WithTags("Orders");

        group.MapPost("/", async ([FromBody] CreateOrderCommand command, ISender sender, CancellationToken ct) =>
        {
            var createdOrder = await sender.Send(command, ct);
            return Results.Created($"/api/v1/orders/{createdOrder.Id}", createdOrder);
        })
        .WithName("CreateOrder")
        .Produces<OrderEntity>(StatusCodes.Status201Created);

        group.MapGet("/user/{userName}", async (string userName, ISender sender, CancellationToken ct) =>
        {
            var orders = await sender.Send(new GetOrdersQuery(userName), ct);
            return Results.Ok(orders);
        })
        .WithName("GetOrdersByUserName")
        .Produces<IReadOnlyList<OrderEntity>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var order = await sender.Send(new GetOrderByIdQuery(id), ct);
            return order is not null ? Results.Ok(order) : Results.NotFound();
        })
        .WithName("GetOrderById")
        .Produces<OrderEntity>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var cancelled = await sender.Send(new CancelOrderCommand(id), ct);
            return cancelled ? Results.Ok() : Results.NotFound();
        })
        .WithName("CancelOrder")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
