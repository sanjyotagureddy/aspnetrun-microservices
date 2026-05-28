namespace Products.Api.Features.Products.GetById;

internal sealed record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductResponse>>;
