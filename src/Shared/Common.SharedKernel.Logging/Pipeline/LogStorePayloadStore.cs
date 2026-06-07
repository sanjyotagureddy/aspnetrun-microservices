using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace Common.SharedKernel.Logging;

internal sealed class LogStorePayloadStore(LogStoreSinkOptions options) : IPayloadStore
{
    private readonly LogStoreSinkOptions _options = Guard.Against.Null(options);
    private readonly HttpClient _httpClient = new();
    private readonly ConcurrentDictionary<string, PayloadStoreWriteResult> _dedupCache = new(StringComparer.Ordinal);
    private readonly Uri _resolvedEndpoint = LogStoreEndpointResolver.Resolve(options.Endpoint);

    internal LogStorePayloadStore(LogStoreSinkOptions options, HttpClient httpClient) : this(options)
    {
        _httpClient = Guard.Against.Null(httpClient);
    }

    public async Task<PayloadStoreWriteResult> StoreAsync(PayloadStoreWriteRequest request, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(request);

        JsonNode protectedPayload = NormalizeDocument(request.ProtectedPayload);
        string serializedDocument = protectedPayload.ToJsonString();
        byte[] bytes = Encoding.UTF8.GetBytes(serializedDocument);
        string payloadHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        if (_options.EnablePayloadDeduplication
            && _dedupCache.TryGetValue(payloadHash, out PayloadStoreWriteResult? cachedResult))
        {
            return cachedResult;
        }

        JsonNode document = BuildPayloadDocument(protectedPayload, request);

        JsonObject payload = new()
        {
            ["document"] = document
        };

        Uri route = new(_resolvedEndpoint, _options.CreateRoutePath);
        using StringContent content = new(payload.ToJsonString(), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await _httpClient.PostAsync(route, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        JsonNode? responseNode = JsonNode.Parse(json);
        string id = responseNode?["id"]?.GetValue<string>()
            ?? throw new InvalidOperationException("LogStore create response does not contain id.");

        PayloadStoreWriteResult result = new(
            PayloadRef: BuildPayloadRef(id),
            PayloadHash: payloadHash,
            PayloadSizeBytes: bytes.LongLength,
            PayloadEncoding: "application/json",
            Compressed: false,
            Encrypted: false);

        if (_options.EnablePayloadDeduplication)
        {
            if (_dedupCache.Count >= _options.MaxPayloadDedupEntries)
            {
                _dedupCache.Clear();
            }

            _dedupCache[payloadHash] = result;
        }

        return result;
    }

    private static JsonNode NormalizeDocument(object protectedPayload)
    {
        if (protectedPayload is JsonNode node)
        {
            return node;
        }

        if (protectedPayload is string text)
        {
            JsonNode? parsed = JsonNode.Parse(text);
            if (parsed is not null)
            {
                return parsed;
            }

            return JsonValue.Create(text);
        }

        return JsonSerializer.SerializeToNode(protectedPayload)
               ?? JsonValue.Create(string.Empty);
    }

    private static JsonNode BuildPayloadDocument(JsonNode protectedPayload, PayloadStoreWriteRequest request)
    {
        return new JsonObject
        {
            ["logType"] = "payload",
            ["contentType"] = request.ContentType,
            ["correlationId"] = request.CorrelationId,
            ["traceId"] = request.TraceId,
            ["timestampUtc"] = DateTimeOffset.UtcNow,
            ["payload"] = protectedPayload
        };
    }

    private string BuildPayloadRef(string id)
    {
        string basePath = _options.CreateRoutePath.TrimEnd('/');
        if (!basePath.StartsWith('/'))
        {
            basePath = "/" + basePath;
        }

        Uri getUri = new(_resolvedEndpoint, $"{basePath}/{id}");
        return getUri.ToString();
    }
}
