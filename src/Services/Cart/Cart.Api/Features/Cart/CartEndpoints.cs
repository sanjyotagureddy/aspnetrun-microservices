using Cart.Api.Domain;
using Cart.Api.Features.Cart.Checkout;
using Cart.Api.Features.Cart.DeleteCart;
using Cart.Api.Features.Cart.GetCart;
using Cart.Api.Features.Cart.UpdateCart;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cart.Api.Features.Cart;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/cart")
            .WithTags("Cart");

        group.MapGet("/{userName}", async (string userName, ISender sender, CancellationToken ct) =>
        {
            var cart = await sender.Send(new GetCartQuery(userName), ct);
            return Results.Ok(cart);
        })
        .WithName("GetCart")
        .Produces<ShoppingCart>(StatusCodes.Status200OK);

        group.MapPost("/", async ([FromBody] ShoppingCart cart, ISender sender, CancellationToken ct) =>
        {
            var updatedCart = await sender.Send(new UpdateCartCommand(cart), ct);
            return Results.Ok(updatedCart);
        })
        .WithName("UpdateCart")
        .Produces<ShoppingCart>(StatusCodes.Status200OK);

        group.MapDelete("/{userName}", async (string userName, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteCartCommand(userName), ct);
            return Results.Ok(result);
        })
        .WithName("DeleteCart")
        .Produces<bool>(StatusCodes.Status200OK);

        group.MapPost("/checkout", async ([FromBody] CartCheckoutDto checkoutDto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CheckoutCartCommand(checkoutDto), ct);
            return result ? Results.Accepted() : Results.BadRequest("Cart is empty or checkout failed.");
        })
        .WithName("CheckoutCart")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest);

        return app;
    }
}
