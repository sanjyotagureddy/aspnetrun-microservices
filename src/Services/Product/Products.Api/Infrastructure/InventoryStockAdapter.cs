using System.Net.Http.Json;

namespace Products.Api.Infrastructure;

internal sealed class InventoryStockAdapter(HttpClient httpClient, ILogger<InventoryStockAdapter> logger) : IInventoryStockAdapter
{
    public async Task InitializeAsync(Guid productId, int stockQuantity, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PutAsJsonAsync($"/api/v1/inventory/{productId}/initialize", new InitializeInventoryRequest(stockQuantity), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<int?> GetStockQuantityAsync(Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            InventoryStockResponse? response = await httpClient.GetFromJsonAsync<InventoryStockResponse>($"/api/v1/inventory/{productId}", cancellationToken);
            return response?.StockQuantity;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch inventory quantity for product {ProductId}", productId);
            return null;
        }
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetStockQuantitiesAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        Guid[] distinctProductIds = productIds.Distinct().ToArray();

        try
        {
            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                "/api/v1/inventory/batch",
                new InventoryBatchRequest(distinctProductIds),
                cancellationToken);

            response.EnsureSuccessStatusCode();

            InventoryBatchResponse? payload = await response.Content.ReadFromJsonAsync<InventoryBatchResponse>(cancellationToken);
            IReadOnlyDictionary<Guid, int> stockByProductId = payload?.StockByProductId ?? new Dictionary<Guid, int>();

            return distinctProductIds.ToDictionary(
                productId => productId,
                productId => stockByProductId.TryGetValue(productId, out int quantity) ? quantity : 0);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch inventory quantities in batch for {ProductCount} products", distinctProductIds.Length);
            return distinctProductIds.ToDictionary(productId => productId, _ => 0);
        }
    }

    private sealed record InitializeInventoryRequest(int StockQuantity);

    private sealed record InventoryStockResponse(Guid ProductId, int StockQuantity);

    private sealed record InventoryBatchRequest(IReadOnlyCollection<Guid> ProductIds);

    private sealed record InventoryBatchResponse(IReadOnlyDictionary<Guid, int> StockByProductId);
}
