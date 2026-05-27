using Common.SharedKernel.Logging;
using Moq;

using Products.Api.Domain;
using Products.Api.Features.Products.Create;
using Products.Api.Features.Products.Get;
using Products.Api.Infrastructure;

namespace Products.Api.Tests;

public sealed class ProductFeatureTests
{
    [Fact]
    public async Task CreateProductHandler_ShouldCreateProductWithNormalizedFields()
    {
        var store = new FakeProductCatalogStore();
        var handler = new CreateProductCommandHandler(store, new FixedTimeProvider(new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.Zero)), new Mock<ILogger<CreateProductCommandHandler>>().Object);

        var command = new CreateProductCommand(
            "Laptop",
            "Business laptop",
            "sku-001",
            1299.99m,
            "usd",
            "Electronics",
            "Contoso",
            10,
            true);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("SKU-001", result.Value!.Sku);
        Assert.Equal("USD", result.Value.Currency);
        Assert.Equal(new DateTime(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc), result.Value.CreatedAt);
        Assert.Single(store.Products);
    }

    [Fact]
    public async Task CreateProductValidator_ShouldRejectInvalidCommand()
    {
        var validator = new CreateProductCommandValidator();

        var result = await validator.ValidateAsync(new CreateProductCommand(
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            "us",
            string.Empty,
            string.Empty,
            -1,
            true), TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Fact]
    public async Task GetProductsHandler_ShouldReturnPagedResults()
    {
        var store = new FakeProductCatalogStore();
        var createdAt = new DateTime(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc);
        var product = new Product(Guid.NewGuid(), "Keyboard", "Mechanical keyboard", "SKU-KEY", 79.99m, "USD", "Accessories", "Contoso", 25, true, createdAt);
        store.Products.Add(product);

        var handler = new GetProductsQueryHandler(store);

        var result = await handler.Handle(new GetProductsQuery(null, null, null, true, 1, 20), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(1, result.Value.TotalCount);
    }

    private sealed class FakeProductCatalogStore : IProductCatalogStore
    {
        public List<Product> Products { get; } = [];

        public Task AddAsync(Product product, CancellationToken cancellationToken)
        {
            Products.Add(product);
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            int removed = Products.RemoveAll(product => product.Id == id);
            return Task.FromResult(removed > 0);
        }

        public Task EnsureSkuIsUniqueAsync(string sku, Guid? productId, CancellationToken cancellationToken)
        {
            bool exists = Products.Any(product => product.Sku == sku && product.Id != productId);
            if (exists)
            {
                throw new Common.SharedKernel.Exceptions.ConflictException($"Product SKU '{sku}' already exists.");
            }

            return Task.CompletedTask;
        }

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Product? product = Products.FirstOrDefault(product => product.Id == id);
            return Task.FromResult(product);
        }

        public Task<ProductSearchResult> SearchAsync(ProductSearchFilter filter, CancellationToken cancellationToken)
        {
            IEnumerable<Product> query = Products;

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string search = filter.Search.Trim();
                query = query.Where(product =>
                    product.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    product.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    product.Sku.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                string category = filter.Category.Trim();
                query = query.Where(product => product.Category.IndexOf(category, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!string.IsNullOrWhiteSpace(filter.Brand))
            {
                string brand = filter.Brand.Trim();
                query = query.Where(product => product.Brand.IndexOf(brand, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(product => product.IsActive == filter.IsActive.Value);
            }

            List<Product> orderedProducts = query
                .OrderBy(product => product.Name)
                .ThenBy(product => product.Sku)
                .ToList();

            int startIndex = (filter.Page - 1) * filter.PageSize;
            List<Product> items = startIndex >= orderedProducts.Count
                ? []
                : orderedProducts.Skip(startIndex).Take(filter.PageSize).ToList();

            int totalCount = orderedProducts.Count;
            return Task.FromResult(new ProductSearchResult(items, totalCount));
        }

        public Task UpdateAsync(Product product, CancellationToken cancellationToken)
        {
            int index = Products.FindIndex(existing => existing.Id == product.Id);
            if (index >= 0)
            {
                Products[index] = product;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
