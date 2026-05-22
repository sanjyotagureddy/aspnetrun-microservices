using System.Net;
using System.Net.Http.Json;
using Catalog.API.Application.Contracts.Persistence;
using Catalog.API.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SharedKernel;
using SharedKernel.Exceptions;
using Xunit;

namespace Catalog.API.Test;

public class CatalogEndpointsIntegrationTest
{
    [Fact]
    public async Task GetAllProducts_ReturnsOkAndContainsProducts()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<Product>>();
        list.Should().NotBeNull();
        list.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAllProducts_InProductionEnvironment_ReturnsOk()
    {
        using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProductById_WithExistingId_ReturnsOk()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/catalog/products/{KnownProductId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProduct_WithValidProduct_ReturnsCreatedAndLocation()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var createProduct = new
        {
            name = "New",
            category = "X",
            summary = "Summary",
            description = "Description",
            imageFile = "image.png",
            price = 12.34m
        };

        var response = await client.PostAsJsonAsync("/api/v1/catalog/products", createProduct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location?.AbsolutePath.Should().Contain("/api/v1/catalog/products/");
    }

    [Fact]
    public async Task CreateProduct_WithSameIdempotencyKey_ReplaysCachedResponseAndCallsRepositoryOnce()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var product = new
        {
            name = "Idempotent",
            category = "X",
            summary = "Summary",
            description = "Description",
            imageFile = "image.png",
            price = 99.99m
        };

        var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/catalog/products")
        {
            Content = JsonContent.Create(product)
        };
        firstRequest.Headers.Add("Idempotency-Key", "idem-create-001");

        var firstResponse = await client.SendAsync(firstRequest);

        var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/catalog/products")
        {
            Content = JsonContent.Create(product)
        };
        secondRequest.Headers.Add("Idempotency-Key", "idem-create-001");

        var secondResponse = await client.SendAsync(secondRequest);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        firstResponse.Headers.Location?.AbsolutePath.Should().Be(secondResponse.Headers.Location?.AbsolutePath);
        ProductRepositoryMock.Verify(r => r.CreateProduct(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProductById_WithMissingId_ReturnsNotFound()
    {
        using var factory = CreateFactory();
        var missingId = Guid.Parse("22222222-2222-2222-2222-222222222200");
        ProductRepositoryMock.Setup(r => r.GetProduct(missingId, It.IsAny<CancellationToken>())).ReturnsAsync(default(Product));
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/catalog/products/{missingId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProductById_WhenRepositoryThrowsBaseException_ReturnsErrorPayload()
    {
        using var factory = CreateFactory();
        var id = Guid.Parse("33333333-3333-3333-3333-333333333300");
        ProductRepositoryMock.Setup(r => r.GetProduct(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(Constants.ServiceCodes.Catalog, "Item not found (repo)"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/catalog/products/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        doc.RootElement.GetProperty("code").GetString().Should().NotBeNullOrWhiteSpace();
        doc.RootElement.GetProperty("description").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetProductsByCategory_WithNoMatches_ReturnsOkAndEmptyList()
    {
        using var factory = CreateFactory();
        var category = "NoMatchCategory";
        ProductRepositoryMock.Setup(r => r.GetProductsByCategory(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Product>());
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/catalog/products?category={Uri.EscapeDataString(category)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<Product>>();
        list.Should().NotBeNull();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateProduct_WhenRepositoryReturnsFalse_ReturnsOkWithFalseBody()
    {
        using var factory = CreateFactory();
        ProductRepositoryMock.Setup(r => r.UpdateProduct(It.IsAny<Product>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var product = new Product { Id = Guid.NewGuid(), Name = "New", Category = "X" };
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/v1/catalog/products/{product.Id}", product);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<bool>();
        body.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProduct_WhenRepositoryReturnsFalse_ReturnsOkWithFalseBody()
    {
        using var factory = CreateFactory();
        var id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        ProductRepositoryMock.Setup(r => r.DeleteProduct(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/catalog/products/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<bool>();
        body.Should().BeFalse();
    }

    [Fact]
    public async Task CreateProduct_WithInvalidProduct_ReturnsBadRequest()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var product = new
        {
            name = string.Empty,
            category = string.Empty,
            summary = string.Empty,
            description = string.Empty,
            imageFile = string.Empty,
            price = -1m
        };

        var response = await client.PostAsJsonAsync("/api/v1/catalog/products", product);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static readonly Guid KnownProductId = Guid.Parse("11111111-1111-1111-1111-111111111115");
    private static readonly List<Product> Products =
    [
        new Product { Id = KnownProductId, Name = "IPhone X", Category = "Smart Phone" }
    ];

    private Mock<IProductRepository> ProductRepositoryMock { get; set; } = null!;

    private WebApplicationFactory<Program> CreateFactory(string environment = "Development")
    {
        ProductRepositoryMock = new Mock<IProductRepository>();
        ProductRepositoryMock.Setup(r => r.GetProducts(It.IsAny<CancellationToken>())).ReturnsAsync(() => Products);
        ProductRepositoryMock.Setup(r => r.GetProduct(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => Products.FirstOrDefault(product => product.Id == id));
        ProductRepositoryMock.Setup(r => r.GetProductsByName(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken _) => Products.Where(product => product.Name == name));
        ProductRepositoryMock.Setup(r => r.GetProductsByCategory(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string category, CancellationToken _) => Products.Where(product => product.Category == category));
        ProductRepositoryMock.Setup(r => r.CreateProduct(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ProductRepositoryMock.Setup(r => r.UpdateProduct(It.IsAny<Product>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        ProductRepositoryMock.Setup(r => r.DeleteProduct(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureServices(services =>
            {
                var descriptor = services.FirstOrDefault(service => service.ServiceType == typeof(IProductRepository));
                if (descriptor is not null)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton(ProductRepositoryMock.Object);
            });
        });
    }
}