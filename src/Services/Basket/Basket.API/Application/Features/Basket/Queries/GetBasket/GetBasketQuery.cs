using Basket.API.Domain.Entities;
using MediatR;

namespace Basket.API.Application.Features.Basket.Queries.GetBasket;

public sealed record GetBasketQuery(string UserName) : IRequest<ShoppingCart>;