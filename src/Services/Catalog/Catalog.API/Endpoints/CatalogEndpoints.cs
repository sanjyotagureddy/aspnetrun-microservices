using Catalog.API.Application.Features.Products.Commands.CreateProduct;
using Catalog.API.Application.Features.Products.Commands.DeleteProduct;
using Catalog.API.Application.Features.Products.Commands.UpdateProduct;
using Catalog.API.Application.Features.Products.Queries.GetProductById;
using Catalog.API.Application.Features.Products.Queries.GetProducts;
using Catalog.API.Domain.Entities;

using MediatR;

using SharedKernel.Web;
using SharedKernel.Errors;

namespace Catalog.API.Endpoints;

[EndpointScope([EndpointScope.Development, EndpointScope.Development])]
internal sealed class CatalogEndpoints(ILogger<CatalogEndpoints> logger) : IEndpoint
{
    private readonly ILogger<CatalogEndpoints> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder products = app.MapGroup("api/v1/catalog/products")
            .WithTags("Products");

        products.MapGet(string.Empty,
                async (IMediator mediator, string? name, string? category, CancellationToken cancellationToken) =>
                {
                    IEnumerable<Product> response =
                        await mediator.Send(new GetProductsQuery(name, category), cancellationToken);
                    return Results.Ok(response);
                })
            .WithName("GetProducts");

        products.MapGet("/{id:guid}", async (IMediator mediator, Guid id, CancellationToken cancellationToken) =>
            {
                Product? product = await mediator.Send(new GetProductByIdQuery(id), cancellationToken);
                if (product is not null)
                    return Results.Ok(product);

                _logger.LogError("product with id: {id}, not found.", id);
                throw Errors.Common.NotFound($"product with id: {id}, not found.", new[] { new Info("catalog.product_not_found", id.ToString()) });
            })
            .WithName("GetProductById");

        products.MapPost(string.Empty,
                async (IMediator mediator, CreateProductCommand command, CancellationToken cancellationToken) =>
                {
                    Product createdProduct = await mediator.Send(command, cancellationToken);
                    return Results.CreatedAtRoute("GetProductById", new { id = createdProduct.Id }, createdProduct);
                })
            .WithName("CreateProduct");

        products.MapPut("/{id:guid}",
                async (IMediator mediator, Guid id, Product product, CancellationToken cancellationToken) =>
                {
                    if (id != product.Id)
                        throw Errors.Common.Validation("Route id must match payload id.", new[] { new Info("catalog.invalid_route_id", "Route id must match payload id.") });

                    return Results.Ok((object?)await mediator.Send(new UpdateProductCommand(product), cancellationToken));
                })
            .WithName("UpdateProductById");

        products.MapDelete("/{id:guid}", async (IMediator mediator, Guid id, CancellationToken cancellationToken) =>
                Results.Ok(await mediator.Send(new DeleteProductCommand(id), cancellationToken)))
            .WithName("DeleteProduct");
    }
}
