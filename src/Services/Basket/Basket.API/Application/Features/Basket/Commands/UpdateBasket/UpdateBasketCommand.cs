using Basket.API.Domain.Entities;
using MediatR;

namespace Basket.API.Application.Features.Basket.Commands.UpdateBasket;

public sealed record UpdateBasketCommand(ShoppingCart Basket) : IRequest<ShoppingCart>;