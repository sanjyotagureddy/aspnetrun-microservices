using System.Text.Json;
using Common.SharedKernel.Logging;
using Common.SharedKernel.Messaging;
using Moq;
using Products.Api.Domain;
using Products.Api.Features.Products.Create;
using Products.Api.Features.Products.Events;
using Products.Api.Features.Products.Get;
using Products.Api.Infrastructure;
using Products.Api.Infrastructure.Outbox;
using Products.Api.Infrastructure.Persistence;

namespace Products.Api.Tests;

public sealed class ProductFeatureTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreateProductHandler_ShouldCreateProductWithNormalizedFields()
    {
        var store = new FakeProductCatalogStore();
        var outboxStore = new FakeProductOutboxStore();
        var dispatcher = new ProductDomainEventDispatcher(outboxStore);
        var inventory = new FakeInventoryStockAdapter();
        var handler = new CreateProductCommandHandler(
            store,
            inventory,
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.Zero)),
            new Mock<ILogger<CreateProductCommandHandler>>().Object,
            dispatcher,
            new FakeProductTransactionExecutor(store, outboxStore));

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
        Assert.Equal(10, result.Value.StockQuantity);
        Assert.Single(store.Products);
        Assert.Single(outboxStore.Messages);
        ProductOutboxMessage outboxMessage = outboxStore.Messages[0];
        Assert.Equal(ProductCreatedIntegrationEvent.Topic, outboxMessage.Topic);

        var integrationEvent = JsonSerializer.Deserialize<ProductCreatedIntegrationEvent>(outboxMessage.PayloadJson, JsonOptions)!;
        ProductOutboxMetadata metadata = JsonSerializer.Deserialize<ProductOutboxMetadata>(outboxMessage.MetadataJson, JsonOptions)!;

        Assert.NotEqual(Guid.Empty, integrationEvent.EventId);
        Assert.Equal(result.Value.Id, integrationEvent.ProductId);
        Assert.Equal("SKU-001", integrationEvent.Sku);
        Assert.Equal("USD", integrationEvent.Currency);
        Assert.Equal(new DateTime(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc), integrationEvent.OccurredOnUtc);
        Assert.Equal(integrationEvent.EventId.ToString("N"), metadata.MessageId);
        Assert.Equal(result.Value.Id.ToString("N"), metadata.RoutingKey);
        Assert.Equal(result.Value.Id.ToString("N"), metadata.OrderingKey);
        Assert.Equal("products-api", metadata.Headers["Source"]);
        Assert.Equal(ProductCreatedIntegrationEvent.EventTypeName, metadata.Headers["EventType"]);

        SystemTextJsonMessageSerializer serializer = new();
        MessageMetadata envelopeMetadata = new()
        {
            MessageId = metadata.MessageId,
            CorrelationId = metadata.CorrelationId,
            CausationId = metadata.CausationId,
            TraceId = metadata.TraceId,
            SpanId = metadata.SpanId,
            TenantId = metadata.TenantId,
            RoutingKey = metadata.RoutingKey,
            OrderingKey = metadata.OrderingKey,
            Contract = new MessageContractDescriptor(
                metadata.Contract.MessageType,
                metadata.Contract.Version,
                metadata.Contract.ContentType,
                Compatibility: metadata.Contract.Compatibility)
        };

        foreach (KeyValuePair<string, string> header in metadata.Headers)
        {
            envelopeMetadata.Headers[header.Key] = header.Value;
        }

        foreach (KeyValuePair<string, string> hint in metadata.TransportHints)
        {
            envelopeMetadata.TransportHints[hint.Key] = hint.Value;
        }

        MessageEnvelope<ProductCreatedIntegrationEvent> envelope = MessageEnvelope<ProductCreatedIntegrationEvent>.Create(
            ProductCreatedIntegrationEvent.Topic,
            integrationEvent,
            envelopeMetadata);
        IMessageEnvelope<ProductCreatedIntegrationEvent> restored = serializer.Deserialize<ProductCreatedIntegrationEvent>(serializer.Serialize(envelope));
        Assert.Equal(integrationEvent.EventId, restored.Payload.EventId);
        Assert.Equal(integrationEvent.ProductId, restored.Payload.ProductId);
    }

    [Fact]
    public async Task CreateProductHandler_ShouldPersistProductAndOutbox_WhenInventoryInitializationFailsAfterCommit()
    {
        var store = new FakeProductCatalogStore();
        var outboxStore = new FakeProductOutboxStore();
        var dispatcher = new ProductDomainEventDispatcher(outboxStore);
        var inventory = new FakeInventoryStockAdapter { ThrowOnInitialize = true };
        var handler = new CreateProductCommandHandler(
            store,
            inventory,
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.Zero)),
            new Mock<ILogger<CreateProductCommandHandler>>().Object,
            dispatcher,
            new FakeProductTransactionExecutor(store, outboxStore));

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
        Assert.Equal(10, result.Value!.StockQuantity);
        Assert.Single(store.Products);
        Assert.Single(outboxStore.Messages);
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
        var inventory = new FakeInventoryStockAdapter();
        var createdAt = new DateTime(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc);
        var product = new Product(Guid.NewGuid(), "Keyboard", "Mechanical keyboard", "SKU-KEY", 79.99m, "USD", "Accessories", "Contoso", true, createdAt);
        store.Products.Add(product);
        await inventory.InitializeAsync(product.Id, 25, CancellationToken.None);

        var handler = new GetProductsQueryHandler(store, inventory);

        var result = await handler.Handle(new GetProductsQuery(null, null, null, true, 1, 20), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(25, result.Value.Items.First().StockQuantity);
    }

    private sealed class FakeInventoryStockAdapter : IInventoryStockAdapter
    {
        private readonly Dictionary<Guid, int> _stockByProductId = new();

        public bool ThrowOnInitialize { get; init; }

        public Task InitializeAsync(Guid productId, int stockQuantity, CancellationToken cancellationToken)
        {
            if (ThrowOnInitialize)
            {
                throw new InvalidOperationException("Inventory initialization failed.");
            }

            _stockByProductId.TryAdd(productId, stockQuantity);
            return Task.CompletedTask;
        }

        public Task<int?> GetStockQuantityAsync(Guid productId, CancellationToken cancellationToken)
        {
            int? stockQuantity = _stockByProductId.TryGetValue(productId, out int value)
                ? value
                : null;

            return Task.FromResult(stockQuantity);
        }

        public Task<IReadOnlyDictionary<Guid, int>> GetStockQuantitiesAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
        {
            Dictionary<Guid, int> values = productIds.Distinct().ToDictionary(
                productId => productId,
                productId => _stockByProductId.GetValueOrDefault(productId, 0));

            return Task.FromResult((IReadOnlyDictionary<Guid, int>)values);
        }
    }

    private sealed class FakeProductCatalogStore : IProductCatalogStore
    {
        public List<Product> Products { get; } = [];

        public Task AddAsync(Product product, CancellationToken cancellationToken)
        {
            Products.Add(product);
            return Task.CompletedTask;
        }

        Task IProductCatalogStore.AddAsync(Product product, Npgsql.NpgsqlConnection connection, Npgsql.NpgsqlTransaction transaction, CancellationToken cancellationToken)
            => AddAsync(product, cancellationToken);

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            int removed = Products.RemoveAll(product => product.Id == id);
            return Task.FromResult(removed > 0);
        }

        Task<bool> IProductCatalogStore.DeleteAsync(Guid id, Npgsql.NpgsqlConnection connection, Npgsql.NpgsqlTransaction transaction, CancellationToken cancellationToken)
            => DeleteAsync(id, cancellationToken);

        public Task EnsureSkuIsUniqueAsync(string sku, Guid? productId, CancellationToken cancellationToken)
        {
            bool exists = Products.Any(product => product.Sku == sku && product.Id != productId);
            if (exists)
            {
                throw new Common.SharedKernel.Exceptions.ConflictException($"Product SKU '{sku}' already exists.");
            }

            return Task.CompletedTask;
        }

        Task IProductCatalogStore.EnsureSkuIsUniqueAsync(string sku, Guid? productId, Npgsql.NpgsqlConnection connection, Npgsql.NpgsqlTransaction transaction, CancellationToken cancellationToken)
            => EnsureSkuIsUniqueAsync(sku, productId, cancellationToken);

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

        Task IProductCatalogStore.UpdateAsync(Product product, Npgsql.NpgsqlConnection connection, Npgsql.NpgsqlTransaction transaction, CancellationToken cancellationToken)
            => UpdateAsync(product, cancellationToken);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class FakeProductOutboxStore : IProductOutboxStore
    {
        public List<ProductOutboxMessage> Messages { get; } = [];

        public Task EnqueueAsync(ProductOutboxMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task EnqueueAsync(ProductOutboxMessage message, Npgsql.NpgsqlConnection connection, Npgsql.NpgsqlTransaction transaction, CancellationToken cancellationToken)
            => EnqueueAsync(message, cancellationToken);

        public Task<IReadOnlyList<ProductOutboxMessage>> ClaimPendingAsync(int batchSize, TimeSpan claimDuration, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyList<ProductOutboxMessage>)Messages.Take(batchSize).ToList());

        public Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task MarkFailedAsync(Guid id, int attemptCount, string error, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeProductTransactionExecutor(FakeProductCatalogStore store, FakeProductOutboxStore outboxStore) : IProductTransactionExecutor
    {
        public async Task ExecuteAsync(Func<Npgsql.NpgsqlConnection, Npgsql.NpgsqlTransaction, CancellationToken, Task> operation, CancellationToken cancellationToken)
        {
            List<Product> productSnapshot = [.. store.Products];
            List<ProductOutboxMessage> outboxSnapshot = [.. outboxStore.Messages];

            try
            {
                await operation(null!, null!, cancellationToken);
            }
            catch
            {
                store.Products.Clear();
                store.Products.AddRange(productSnapshot);
                outboxStore.Messages.Clear();
                outboxStore.Messages.AddRange(outboxSnapshot);
                throw;
            }
        }

        public Task<T> ExecuteAsync<T>(Func<Npgsql.NpgsqlConnection, Npgsql.NpgsqlTransaction, CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
            => operation(null!, null!, cancellationToken);
    }
}

