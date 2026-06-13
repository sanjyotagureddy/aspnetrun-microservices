using Common.SharedKernel.Results;
using Inventory.Api.Contracts;
using Inventory.Api.Domain;
using Inventory.Api.Features.Inventory.GetByProductId;
using Inventory.Api.Infrastructure;
using Npgsql;

namespace Inventory.Api.Tests.Features.Inventory.GetByProductId;

public sealed class GetInventoryByProductIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnZero_WhenItemDoesNotExist()
    {
        FakeInventoryStore store = new();
        GetInventoryByProductIdQueryHandler handler = new(store);
        Guid productId = Guid.NewGuid();

        Result<InventoryResponse> result = await handler.Handle(new GetInventoryByProductIdQuery(productId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        InventoryResponse response = result.Value!;
        Assert.Equal(productId, response.ProductId);
        Assert.Equal(0, response.StockQuantity);
    }

    [Fact]
    public async Task Handle_ShouldReturnStock_WhenItemExists()
    {
        Guid productId = Guid.NewGuid();
        FakeInventoryStore store = new()
        {
            Item = new InventoryItem(productId, 22, DateTime.UtcNow)
        };
        GetInventoryByProductIdQueryHandler handler = new(store);

        Result<InventoryResponse> result = await handler.Handle(new GetInventoryByProductIdQuery(productId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(22, result.Value!.StockQuantity);
    }

    private sealed class FakeInventoryStore : IInventoryStore
    {
        public InventoryItem? Item { get; set; }

        public Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
            => Task.FromResult(Item);

        public Task<IReadOnlyDictionary<Guid, int>> GetStockByProductIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyDictionary<Guid, int>)new Dictionary<Guid, int>());

        public Task InitializeAsync(InventoryItem item, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task InitializeAsync(InventoryItem item, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
