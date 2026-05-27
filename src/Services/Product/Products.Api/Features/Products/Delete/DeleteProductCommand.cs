namespace Products.Api.Features.Products.Delete;

internal sealed record DeleteProductCommand(Guid Id) : IRequest<Result>;
