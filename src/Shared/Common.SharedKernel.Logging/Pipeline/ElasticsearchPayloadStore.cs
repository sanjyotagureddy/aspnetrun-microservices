using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Common.SharedKernel;

namespace Common.SharedKernel.Logging;

internal sealed class ElasticsearchPayloadStore(ElasticsearchSinkOptions options) : IPayloadStore
{
    private readonly ElasticsearchSinkOptions _options = Guard.Against.Null(options);
    private readonly HttpClient _httpClient = new();

    public async Task<PayloadStoreWriteResult> StoreAsync(PayloadStoreWriteRequest request, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(request);

        JsonNode payload = NormalizeDocument(request.ProtectedPayload);
        string serializedPayload = payload.ToJsonString();
        byte[] bytes = Encoding.UTF8.GetBytes(serializedPayload);
        string payloadHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        string index = BuildPayloadIndexName();
        string id = Guid.NewGuid().ToString("N");

        JsonObject document = new()
        {
            ["contentType"] = request.ContentType,
            ["correlationId"] = request.CorrelationId,
            ["traceId"] = request.TraceId,
            ["timestampUtc"] = DateTimeOffset.UtcNow,
            ["payload"] = payload
        };

        using StringContent content = new(document.ToJsonString(), Encoding.UTF8, "application/json");
        Uri docUri = new(_options.Endpoint, $"/{index}/_doc/{id}");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, docUri)
        {
            Content = content
        };

        AddHeader(httpRequest, Constants.Headers.CorrelationId, request.CorrelationId);
        AddHeader(httpRequest, Constants.Headers.TraceId, request.TraceId);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return new PayloadStoreWriteResult(
            PayloadRef: docUri.ToString(),
            PayloadHash: payloadHash,
            PayloadSizeBytes: bytes.LongLength,
            PayloadEncoding: "application/json",
            Compressed: false,
            Encrypted: false);
    }

    private string BuildPayloadIndexName()
    {
        string prefix = string.IsNullOrWhiteSpace(_options.PayloadIndexPrefix)
            ? "api-payload"
            : _options.PayloadIndexPrefix.Trim().TrimEnd('-');

        if (!_options.UseDailyIndexes)
        {
            return prefix;
        }

        return $"{prefix}-{DateTimeOffset.UtcNow:yyyy.MM.dd}";
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
