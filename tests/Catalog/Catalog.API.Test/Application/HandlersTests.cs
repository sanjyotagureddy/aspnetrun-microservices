using Catalog.API.Application.Contracts.Persistence;
using Catalog.API.Application.Features.Products.Commands.CreateProduct;
using Catalog.API.Application.Features.Products.Commands.DeleteProduct;
using Catalog.API.Application.Features.Products.Commands.UpdateProduct;
using Catalog.API.Application.Features.Products.Queries.GetProductById;
using Catalog.API.Application.Features.Products.Queries.GetProducts;
using Catalog.API.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace Catalog.API.Test;

public class HandlersTests
{
    [Fact]
    public async Task CreateProductHandler_CreatesProductAndReturnsGeneratedId()
    {
        var repository = new Mock<IProductRepository>();
        Product? capturedProduct = null;
        repository.Setup(r => r.CreateProduct(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((product, _) => capturedProduct = product)
            .Returns(Task.CompletedTask);

        var handler = new CreateProductCommandHandler(repository.Object);
        var command = new CreateProductCommand("Name", "Category", "Summary", "Description", "image.png", 12.5m);

        var result = await handler.Handle(command, CancellationToken.None);

        capturedProduct.Should().NotBeNull();
        result.Should().BeEquivalentTo(capturedProduct);
        result.Id.Should().NotBe(Guid.Empty);
        result.Name.Should().Be(command.Name);
        result.Category.Should().Be(command.Category);
        repository.Verify(r => r.CreateProduct(result, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductHandler_DelegatesToRepository()
    {
        var repository = new Mock<IProductRepository>();
        repository.Setup(r => r.UpdateProduct(It.IsAny<Product>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new UpdateProductCommandHandler(repository.Object);
        var product = new Product { Id = Guid.NewGuid(), Name = "Name" };

        var result = await handler.Handle(new UpdateProductCommand(product), CancellationToken.None);

        result.Should().BeTrue();
        repository.Verify(r => r.UpdateProduct(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProductHandler_DelegatesToRepository()
    {
        var repository = new Mock<IProductRepository>();
        repository.Setup(r => r.DeleteProduct(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new DeleteProductCommandHandler(repository.Object);
        var id = Guid.NewGuid();

        var result = await handler.Handle(new DeleteProductCommand(id), CancellationToken.None);

        result.Should().BeTrue();
        repository.Verify(r => r.DeleteProduct(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProductsHandler_UsesNameQueryWhenNameIsPresent()
    {
        var repository = new Mock<IProductRepository>();
        repository.Setup(r => r.GetProductsByName("Name", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product { Id = Guid.NewGuid(), Name = "Name" }]);
        var handler = new GetProductsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetProductsQuery("Name", "Category"), CancellationToken.None);

        result.Should().HaveCount(1);
        repository.Verify(r => r.GetProductsByName("Name", It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.GetProductsByCategory(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.GetProducts(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetProductsHandler_UsesCategoryQueryWhenNameIsMissing()
    {
        var repository = new Mock<IProductRepository>();
        repository.Setup(r => r.GetProductsByCategory("Category", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product { Id = Guid.NewGuid(), Category = "Category" }]);
        var handler = new GetProductsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetProductsQuery(null, "Category"), CancellationToken.None);

        result.Should().HaveCount(1);
        repository.Verify(r => r.GetProductsByCategory("Category", It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.GetProducts(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetProductsHandler_UsesDefaultQueryWhenNoFilterExists()
    {
        var repository = new Mock<IProductRepository>();
        repository.Setup(r => r.GetProducts(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product { Id = Guid.NewGuid(), Name = "Default" }]);
        var handler = new GetProductsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetProductsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        repository.Verify(r => r.GetProducts(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProductByIdHandler_DelegatesToRepository()
    {
        var repository = new Mock<IProductRepository>();
        var product = new Product { Id = Guid.NewGuid(), Name = "Name" };
        repository.Setup(r => r.GetProduct(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        var handler = new GetProductByIdQueryHandler(repository.Object);

        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        result.Should().BeEquivalentTo(product);
        repository.Verify(r => r.GetProduct(product.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}