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

        Task<KeyValuePair<Guid, int?>>[] tasks = productIds
            .Distinct()
            .Select(async productId => new KeyValuePair<Guid, int?>(productId, await GetStockQuantityAsync(productId, cancellationToken)))
            .ToArray();

        KeyValuePair<Guid, int?>[] results = await Task.WhenAll(tasks);
        return results.ToDictionary(item => item.Key, item => item.Value ?? 0);
    }

    private sealed record InitializeInventoryRequest(int StockQuantity);

    private sealed record InventoryStockResponse(Guid ProductId, int StockQuantity);
}
