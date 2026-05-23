using MediatR;

namespace Basket.API.Application.Features.Basket.Commands.DeleteBasket;

public sealed record DeleteBasketCommand(string UserName) : IRequest<Unit>;