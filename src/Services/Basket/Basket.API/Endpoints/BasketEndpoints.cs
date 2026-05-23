using Basket.API.Application.Features.Basket.Commands.CheckoutBasket;
using Basket.API.Application.Features.Basket.Commands.DeleteBasket;
using Basket.API.Application.Features.Basket.Commands.UpdateBasket;
using Basket.API.Application.Features.Basket.Queries.GetBasket;
using Basket.API.Domain.Entities;

using MediatR;

using Microsoft.AspNetCore.Http.HttpResults;

using SharedKernel.Web;

namespace Basket.API.Endpoints;

internal sealed class BasketEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder basket = app.MapGroup("api/v1/basket")
            .WithTags("Basket");

        basket.MapGet("{userName}", GetBasket)
            .WithName("GetBasket");

        basket.MapPost(string.Empty, UpdateBasket)
            .WithName("UpdateBasket");

        basket.MapDelete("{userName}", DeleteBasket)
            .WithName("DeleteBasket");

        basket.MapPost("checkout", Checkout)
            .WithName("Checkout");
    }

    internal static async Task<Ok<ShoppingCart>> GetBasket(IMediator mediator, string userName, CancellationToken cancellationToken)
    {
        ShoppingCart basket = await mediator.Send(new GetBasketQuery(userName), cancellationToken);
        return TypedResults.Ok(basket ?? new ShoppingCart(userName));
    }

    internal static async Task<Ok<ShoppingCart>> UpdateBasket(IMediator mediator, ShoppingCart basket, CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new UpdateBasketCommand(basket), cancellationToken));

    internal static async Task<Ok> DeleteBasket(IMediator mediator, string userName, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteBasketCommand(userName), cancellationToken);
        return TypedResults.Ok();
    }

    internal static async Task<IResult> Checkout(IMediator mediator, CheckoutBasketCommand command, CancellationToken cancellationToken)
    {
        await mediator.Send(command, cancellationToken);
        return Results.Accepted();
    }
}