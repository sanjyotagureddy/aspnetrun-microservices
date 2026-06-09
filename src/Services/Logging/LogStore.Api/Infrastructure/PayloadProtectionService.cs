using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Common.SharedKernel;
using Common.SharedKernel.Exceptions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace LogStore.Api.Infrastructure;

internal sealed class PayloadProtectionService(
    HttpClient httpClient,
    IOptions<LogStorageOptions> options,
    ILogger<PayloadProtectionService> logger,
    IHttpContextAccessor httpContextAccessor) : ILogStorageService
{
    private readonly LogStorageOptions _options = options.Value;

    public async Task<CreateLogResponse> CreateAsync(CreateLogRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var id = string.IsNullOrWhiteSpace(request.Id)
            ? Guid.NewGuid().ToString("N")
            : request.Id.Trim();

        JsonNode document = ParseDocument(request.Document);
        EnsureTimestamp(document);

        var index = ResolveTargetIndex(request.IndexPrefix, document);

        using HttpRequestMessage httpRequest = new(HttpMethod.Put, $"{index}/_doc/{id}")
        {
            Content = new StringContent(document.ToJsonString(), Encoding.UTF8, "application/json")
        };

        AddPropagationHeaders(httpRequest, httpContextAccessor.HttpContext);

        using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to persist log document to index '{index}'. Status: {(int)response.StatusCode}. Details: {details}");
        }

        logger.LogInformation("Log document persisted to index {Index} with id {Id}", index, id);
        return new CreateLogResponse(id, index, DateTimeOffset.UtcNow);
    }

    public async Task<GetLogResponse> GetAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationException("Log id is required.", "id");
        }

        var searchIndexes = string.Join(',', BuildSearchPatterns());

        JsonObject searchQuery = new()
        {
            ["size"] = 1,
            ["query"] = new JsonObject
            {
                ["ids"] = new JsonObject
                {
                    ["values"] = new JsonArray(id.Trim())
                }
            }
        };

        using HttpRequestMessage httpRequest = new(HttpMethod.Post, $"{searchIndexes}/_search")
        {
            Content = new StringContent(searchQuery.ToJsonString(), Encoding.UTF8, "application/json")
        };

        AddPropagationHeaders(httpRequest, httpContextAccessor.HttpContext);

        using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to query log document '{id}'. Status: {(int)response.StatusCode}. Details: {details}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var root = JsonNode.Parse(json);
        JsonArray? hits = root?["hits"]?["hits"]?.AsArray();

        if (hits is null || hits.Count == 0)
        {
            throw new NotFoundException("Log", id);
        }

        JsonNode first = hits[0]!;
        var index = first["_index"]?.GetValue<string>() ?? string.Empty;
        JsonNode source = first["_source"] ?? new JsonObject();

        using var sourceDocument = JsonDocument.Parse(source.ToJsonString());
        JsonElement logDocument = sourceDocument.RootElement.Clone();

        return new GetLogResponse(id.Trim(), index, logDocument);
    }

    private static JsonNode ParseDocument(JsonElement document)
    {
        if (document.ValueKind is JsonValueKind.Undefined)
        {
            throw new ValidationException("Log document payload is required.", "document");
        }

        var parsed = JsonNode.Parse(document.GetRawText());
        return parsed ?? throw new ValidationException("Log document payload could not be parsed.", "document");
    }

    private static void EnsureTimestamp(JsonNode document)
    {
        if (document is not JsonObject obj)
        {
            return;
        }

        if (obj["timestampUtc"] is null)
        {
            obj["timestampUtc"] = DateTimeOffset.UtcNow;
        }
    }

    private string ResolveTargetIndex(string? explicitIndexPrefix, JsonNode document)
    {
        var prefix = !string.IsNullOrWhiteSpace(explicitIndexPrefix)
            ? explicitIndexPrefix.Trim().TrimEnd('-')
            : ResolvePrefixFromDocument(document);

        if (!_options.UseDailyIndexes)
        {
            return prefix;
        }

        DateTimeOffset timestamp = ResolveTimestamp(document);
        return $"{prefix}-{timestamp:yyyy.MM.dd}";
    }

    private string ResolvePrefixFromDocument(JsonNode document)
    {
        if (document is not JsonObject obj)
        {
            return _options.ApiIndexPrefix;
        }

        var logType = obj["logType"]?.GetValue<string>();
        if (string.Equals(logType, "payload", StringComparison.OrdinalIgnoreCase))
        {
            return _options.PayloadIndexPrefix;
        }

        if (string.Equals(logType, "infra", StringComparison.OrdinalIgnoreCase))
        {
            return _options.InfraIndexPrefix;
        }

        if (string.Equals(logType, "messaging", StringComparison.OrdinalIgnoreCase)
            || string.Equals(logType, "event", StringComparison.OrdinalIgnoreCase))
        {
            return _options.MessagingIndexPrefix;
        }

        return _options.ApiIndexPrefix;
    }

    private static DateTimeOffset ResolveTimestamp(JsonNode document)
    {
        if (document is JsonObject obj
            && obj["timestampUtc"] is { } timestampNode
            && TryReadTimestamp(timestampNode, out DateTimeOffset parsed))
        {
            return parsed;
        }

        return DateTimeOffset.UtcNow;
    }

    private static bool TryReadTimestamp(JsonNode timestampNode, out DateTimeOffset parsed)
    {
        parsed = default;

        if (timestampNode is JsonValue value)
        {
            if (value.TryGetValue(out DateTimeOffset dto))
            {
                parsed = dto;
                return true;
            }

            if (value.TryGetValue(out DateTime dt))
            {
                parsed = new DateTimeOffset(dt);
                return true;
            }

            if (value.TryGetValue<string>(out var text)
                && !string.IsNullOrWhiteSpace(text)
                && DateTimeOffset.TryParse(text, out DateTimeOffset parsedFromString))
            {
                parsed = parsedFromString;
                return true;
            }
        }

        return false;
    }

    private IEnumerable<string> BuildSearchPatterns()
    {
        yield return BuildPattern(_options.ApiIndexPrefix);
        yield return BuildPattern(_options.PayloadIndexPrefix);
        yield return BuildPattern(_options.InfraIndexPrefix);
        yield return BuildPattern(_options.MessagingIndexPrefix);
    }

    private string BuildPattern(string prefix)
        => _options.UseDailyIndexes ? $"{prefix.TrimEnd('-')}-*" : prefix.TrimEnd('-');

    private static void AddPropagationHeaders(HttpRequestMessage request, HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return;
        }

        CopyHeader(httpContext, request, Constants.Headers.CorrelationId);
        CopyHeader(httpContext, request, Constants.Headers.ParentCorrelationId);
        CopyHeader(httpContext, request, Constants.Headers.TraceId);
        CopyHeader(httpContext, request, Constants.Headers.SpanId);
        CopyHeader(httpContext, request, Constants.Headers.TenantId);
        CopyHeader(httpContext, request, Constants.Headers.CallerService);
    }

    private static void CopyHeader(HttpContext source, HttpRequestMessage target, string headerName)
    {
        if (!source.Request.Headers.TryGetValue(headerName, out var values) || StringValues.IsNullOrEmpty(values))
        {
            return;
        }

        target.Headers.Remove(headerName);
        target.Headers.TryAddWithoutValidation(headerName, values.ToString());
    }
}
