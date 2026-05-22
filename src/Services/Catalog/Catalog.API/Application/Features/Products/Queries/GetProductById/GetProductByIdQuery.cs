using Catalog.API.Domain.Entities;
using MediatR;

namespace Catalog.API.Application.Features.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<Product?>;