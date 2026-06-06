using Inventory.Api.Contracts;
using Common.SharedKernel.Results;
using Inventory.Api.Domain;
using Inventory.Api.Features.Inventory.GetBatch;
using Inventory.Api.Infrastructure;

namespace Inventory.Api.Tests.Features.Inventory.GetBatch;

public sealed class GetInventoryBatchQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnDistinctIds_WithZeroFallback()
    {
        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();
        FakeInventoryStore store = new()
        {
            Stocks = new Dictionary<Guid, int>
            {
                [id1] = 5
            }
        };

        GetInventoryBatchQueryHandler handler = new(store);

        Result<InventoryBatchResponse> result = await handler.Handle(new GetInventoryBatchQuery([id1, id1, id2]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.StockByProductId.Count);
        Assert.Equal(5, result.Value.StockByProductId[id1]);
        Assert.Equal(0, result.Value.StockByProductId[id2]);
    }

    private sealed class FakeInventoryStore : IInventoryStore
    {
        public IReadOnlyDictionary<Guid, int> Stocks { get; set; } = new Dictionary<Guid, int>();

        public Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
            => Task.FromResult<InventoryItem?>(null);

        public Task<IReadOnlyDictionary<Guid, int>> GetStockByProductIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
            => Task.FromResult(Stocks);

        public Task InitializeAsync(InventoryItem item, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
