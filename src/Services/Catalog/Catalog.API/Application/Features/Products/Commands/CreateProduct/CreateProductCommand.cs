using MediatR;

namespace Catalog.API.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
	string Name,
	string Category,
	string Summary,
	string Description,
	string ImageFile,
	decimal Price) : IRequest<Domain.Entities.Product>;