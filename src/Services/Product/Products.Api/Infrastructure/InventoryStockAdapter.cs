using System.Net.Http.Json;
using Common.SharedKernel;
using Common.SharedKernel.Observability.Context;

namespace Products.Api.Infrastructure;

internal sealed class InventoryStockAdapter(HttpClient httpClient, ILogger<InventoryStockAdapter> logger) : IInventoryStockAdapter
{
    public async Task InitializeAsync(Guid productId, int stockQuantity, CancellationToken cancellationToken)
    {
        using JsonContent content = JsonContent.Create(new InitializeInventoryRequest(stockQuantity));
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/inventory/{productId}/initialize")
        {
            Content = content
        };

        AddObservabilityHeaders(request);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<int?> GetStockQuantityAsync(Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/inventory/{productId}");
            AddObservabilityHeaders(request);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            InventoryStockResponse? payload = await response.Content.ReadFromJsonAsync<InventoryStockResponse>(cancellationToken);
            return payload?.StockQuantity;
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
            using JsonContent content = JsonContent.Create(new InventoryBatchRequest(distinctProductIds));
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/inventory/batch")
            {
                Content = content
            };

            AddObservabilityHeaders(request);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

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

    private static void AddObservabilityHeaders(HttpRequestMessage request)
    {
        AppCallContextBase? appContext = AppCallContextBase.Current;
        if (appContext is null)
        {
            return;
        }

        AddHeader(request, Constants.Headers.CorrelationId, appContext.CorrelationId);
        AddHeader(request, Constants.Headers.ParentCorrelationId, appContext.ParentCorrelationId);
        AddHeader(request, Constants.Headers.TraceId, appContext.TraceId);
        AddHeader(request, Constants.Headers.SpanId, appContext.SpanId);

        if (appContext.Headers.TryGetValue(Constants.Headers.TenantId, out string? tenantId))
        {
            AddHeader(request, Constants.Headers.TenantId, tenantId);
        }
    }

    private static void AddHeader(HttpRequestMessage request, string headerName, string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return;
        }

        request.Headers.Remove(headerName);
        request.Headers.TryAddWithoutValidation(headerName, headerValue);
    }
}
