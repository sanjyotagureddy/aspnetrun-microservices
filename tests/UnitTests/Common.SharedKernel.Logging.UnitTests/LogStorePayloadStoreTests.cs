using System.Net;
using System.Text;
using System.Text.Json;

namespace Common.SharedKernel.Logging.UnitTests;

public sealed class LogStorePayloadStoreTests
{
    [Fact]
    public async Task StoreAsync_ShouldDeduplicateByHash_WhenEnabled()
    {
        CountingHandler handler = new();
        HttpClient client = new(handler);
        LogStorePayloadStore store = new(
            new LogStoreSinkOptions
            {
                Endpoint = new Uri("http://localhost:5000"),
                CreateRoutePath = "/api/v1/logs",
                EnablePayloadDeduplication = true,
                MaxPayloadDedupEntries = 100
            },
            client);

        PayloadStoreWriteRequest request = new(
            ProtectedPayload: new { token = "abc", value = 42 },
            ContentType: "application/json");

        PayloadStoreWriteResult first = await store.StoreAsync(request, TestContext.Current.CancellationToken);
        PayloadStoreWriteResult second = await store.StoreAsync(request, TestContext.Current.CancellationToken);

        handler.RequestCount.Should().Be(1);
        first.PayloadRef.Should().Be(second.PayloadRef);
        first.PayloadHash.Should().Be(second.PayloadHash);
    }

    [Fact]
    public async Task StoreAsync_ShouldNotDeduplicate_WhenDisabled()
    {
        CountingHandler handler = new();
        HttpClient client = new(handler);
        LogStorePayloadStore store = new(
            new LogStoreSinkOptions
            {
                Endpoint = new Uri("http://localhost:5000"),
                CreateRoutePath = "/api/v1/logs",
                EnablePayloadDeduplication = false,
                MaxPayloadDedupEntries = 100
            },
            client);

        PayloadStoreWriteRequest request = new(
            ProtectedPayload: new { token = "abc", value = 42 },
            ContentType: "application/json");

        PayloadStoreWriteResult first = await store.StoreAsync(request, TestContext.Current.CancellationToken);
        PayloadStoreWriteResult second = await store.StoreAsync(request, TestContext.Current.CancellationToken);

        handler.RequestCount.Should().Be(2);
        first.PayloadRef.Should().NotBe(second.PayloadRef);
        first.PayloadHash.Should().Be(second.PayloadHash);
    }

    [Fact]
    public async Task StoreAsync_ShouldResolveServiceHost_FromAspireEnvironmentVariable()
    {
        const string environmentKey = "services__log-store-api__http__0";
        string? original = Environment.GetEnvironmentVariable(environmentKey);
        Environment.SetEnvironmentVariable(environmentKey, "http://127.0.0.1:5055");

        try
        {
            CountingHandler handler = new();
            HttpClient client = new(handler);
            LogStorePayloadStore store = new(
                new LogStoreSinkOptions
                {
                    Endpoint = new Uri("http://log-store-api"),
                    CreateRoutePath = "/api/v1/logs",
                    EnablePayloadDeduplication = false,
                    MaxPayloadDedupEntries = 100
                },
                client);

            PayloadStoreWriteRequest request = new(
                ProtectedPayload: new { token = "abc", value = 42 },
                ContentType: "application/json");

            PayloadStoreWriteResult result = await store.StoreAsync(request, TestContext.Current.CancellationToken);

            handler.RequestCount.Should().Be(1);
            result.PayloadRef.Should().StartWith("http://127.0.0.1:5055/api/v1/logs/");
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentKey, original);
        }
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        private int _requestCount;

        public int RequestCount => _requestCount;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            int count = Interlocked.Increment(ref _requestCount);
            string responseJson = JsonSerializer.Serialize(new
            {
                id = $"id-{count}",
                index = "api-logs-2026.06.07",
                storedAtUtc = DateTimeOffset.UtcNow
            });

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
        }
    }
}
