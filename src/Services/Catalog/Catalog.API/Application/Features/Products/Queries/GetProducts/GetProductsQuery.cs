using Catalog.API.Domain.Entities;
using MediatR;

namespace Catalog.API.Application.Features.Products.Queries.GetProducts;

public sealed record GetProductsQuery(string? Name = null, string? Category = null) : IRequest<IEnumerable<Product>>;