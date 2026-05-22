using Catalog.API.Domain.Entities;
using MediatR;

namespace Catalog.API.Application.Features.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(Product Product) : IRequest<bool>;