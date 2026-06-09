using System.Net;
using Common.SharedKernel;
using Common.SharedKernel.Observability.Context;
using Microsoft.Extensions.Logging;
using Products.Api.Infrastructure;
using Products.Api.Observability;

namespace Products.Api.Tests;

public sealed class InventoryStockAdapterTests
{
    [Fact]
    public async Task InitializeAsync_ShouldPropagateObservabilityHeaders()
    {
        RecordingHandler handler = new();
        HttpClient client = new(handler)
        {
            BaseAddress = new Uri("http://inventory-api")
        };

        InventoryStockAdapter adapter = new(client, new LoggerFactory().CreateLogger<InventoryStockAdapter>());

        AppCallContext context = new(
            correlationId: "corr-123",
            parentCorrelationId: "pcorr-123",
            traceId: "trace-123",
            spanId: "span-123",
            tenantId: "tenant-123",
            headers: new Dictionary<string, string>
            {
                [Constants.Headers.TenantId] = "tenant-123"
            });

        using IDisposable scope = AppCallContextBase.BeginScope(context);

        await adapter.InitializeAsync(Guid.NewGuid(), 5, CancellationToken.None);

        Assert.Equal("corr-123", GetHeader(handler.LastHeaders, Constants.Headers.CorrelationId));
        Assert.Equal("pcorr-123", GetHeader(handler.LastHeaders, Constants.Headers.ParentCorrelationId));
        Assert.Equal("trace-123", GetHeader(handler.LastHeaders, Constants.Headers.TraceId));
        Assert.Equal("span-123", GetHeader(handler.LastHeaders, Constants.Headers.SpanId));
        Assert.Equal("tenant-123", GetHeader(handler.LastHeaders, Constants.Headers.TenantId));
    }

    [Fact]
    public async Task GetStockQuantityAsync_WithoutContext_ShouldNotWriteObservabilityHeaders()
    {
        RecordingHandler handler = new("{\"productId\":\"1c7d0f66-e76e-40a6-8d56-c2829974fbc7\",\"stockQuantity\":7}");
        HttpClient client = new(handler)
        {
            BaseAddress = new Uri("http://inventory-api")
        };

        InventoryStockAdapter adapter = new(client, new LoggerFactory().CreateLogger<InventoryStockAdapter>());

        int? quantity = await adapter.GetStockQuantityAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(7, quantity);
        Assert.False(handler.LastHeaders.ContainsKey(Constants.Headers.CorrelationId));
        Assert.False(handler.LastHeaders.ContainsKey(Constants.Headers.TraceId));
        Assert.False(handler.LastHeaders.ContainsKey(Constants.Headers.SpanId));
        Assert.False(handler.LastHeaders.ContainsKey(Constants.Headers.TenantId));
    }

    private static string? GetHeader(IReadOnlyDictionary<string, string> headers, string headerName)
    {
        return headers.TryGetValue(headerName, out string? value) ? value : null;
    }

    private sealed class RecordingHandler(string? responseJson = null) : HttpMessageHandler
    {
        private readonly string? _responseJson = responseJson;

        public IReadOnlyDictionary<string, string> LastHeaders { get; private set; } = new Dictionary<string, string>();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastHeaders = request.Headers.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.FirstOrDefault() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

            HttpResponseMessage response = new(HttpStatusCode.OK);
            if (!string.IsNullOrWhiteSpace(_responseJson))
            {
                response.Content = new StringContent(_responseJson, System.Text.Encoding.UTF8, "application/json");
            }

            return Task.FromResult(response);
        }
    }
}
