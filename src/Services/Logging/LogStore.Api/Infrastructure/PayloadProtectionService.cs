using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Common.SharedKernel.Exceptions;
using Microsoft.Extensions.Options;

namespace LogStore.Api.Infrastructure;

internal sealed class PayloadProtectionService(
    HttpClient httpClient,
    IOptions<LogStorageOptions> options) : ILogStorageService
{
    private static readonly string[] InfraNamespacePrefixes =
    [
        "Microsoft.",
        "System.",
        "Aspire.",
        "Npgsql.",
        "Confluent.",
        "RabbitMQ.",
        "StackExchange.Redis",
        "Yarp."
    ];

    private static readonly string[] InfraCategoryPrefixes =
    [
        "infra.",
        "infrastructure.",
        "system.",
        "microsoft.",
        "host."
    ];

    private static readonly string[] MessagingNamespacePrefixes =
    [
        "Common.SharedKernel.Messaging.",
        "Confluent.Kafka"
    ];

    private static readonly string[] MessagingCategoryPrefixes =
    [
        "messaging.",
        "kafka."
    ];

    private static readonly string[] EventCategoryPrefixes =
    [
        "event.",
        "domain.event.",
        "business.event."
    ];

    private static readonly string[] AuditCategoryPrefixes =
    [
        "audit.",
        "compliance."
    ];

    private static readonly string[] SecurityCategoryPrefixes =
    [
        "security.",
        "auth.",
        "authorization.",
        "authentication."
    ];

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

        using StringContent content = new(document.ToJsonString(), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await httpClient.PutAsync($"{index}/_doc/{id}", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to persist log document to index '{index}'. Status: {(int)response.StatusCode}. Details: {details}");
        }

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

        using StringContent content = new(searchQuery.ToJsonString(), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await httpClient.PostAsync($"{searchIndexes}/_search", content, cancellationToken);
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

        if (IsPayloadSnapshotDocument(obj))
        {
            return _options.PayloadIndexPrefix;
        }

        if (IsSecurityLog(obj))
        {
            return _options.SecurityIndexPrefix;
        }

        if (IsAuditLog(obj))
        {
            return _options.AuditIndexPrefix;
        }

        if (IsMessagingLog(obj) || IsEventLog(obj))
        {
            return _options.MessagingIndexPrefix;
        }

        if (IsInfrastructureLog(obj))
        {
            return _options.InfraIndexPrefix;
        }

        var logType = obj["logType"]?.GetValue<string>();
        if (string.Equals(logType, "infra", StringComparison.OrdinalIgnoreCase))
        {
            return _options.InfraIndexPrefix;
        }

        if (string.Equals(logType, "messaging", StringComparison.OrdinalIgnoreCase)
            || string.Equals(logType, "event", StringComparison.OrdinalIgnoreCase))
        {
            return _options.MessagingIndexPrefix;
        }

        if (string.Equals(logType, "audit", StringComparison.OrdinalIgnoreCase))
        {
            return _options.AuditIndexPrefix;
        }

        if (string.Equals(logType, "security", StringComparison.OrdinalIgnoreCase))
        {
            return _options.SecurityIndexPrefix;
        }

        return _options.ApiIndexPrefix;
    }

    private static bool IsInfrastructureLog(JsonObject document)
    {
        var logNamespace = document["namespace"]?.GetValue<string>();
        if (StartsWithAny(logNamespace, InfraNamespacePrefixes))
        {
            return true;
        }

        var category = document["category"]?.GetValue<string>();
        if (StartsWithAny(category, InfraCategoryPrefixes))
        {
            return true;
        }

        return IsLogType(document, "infra");
    }

    private static bool IsMessagingLog(JsonObject document)
    {
        var logNamespace = document["namespace"]?.GetValue<string>();
        if (StartsWithAny(logNamespace, MessagingNamespacePrefixes))
        {
            return true;
        }

        var category = document["category"]?.GetValue<string>();
        if (StartsWithAny(category, MessagingCategoryPrefixes))
        {
            return true;
        }

        if (IsLogType(document, "messaging"))
        {
            return true;
        }

        var provider = document["provider"]?.GetValue<string>();
        return string.Equals(provider, "Kafka", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEventLog(JsonObject document)
    {
        var category = document["category"]?.GetValue<string>();
        if (StartsWithAny(category, EventCategoryPrefixes))
        {
            return true;
        }

        return IsLogType(document, "event");
    }

    private static bool IsAuditLog(JsonObject document)
    {
        var category = document["category"]?.GetValue<string>();
        if (StartsWithAny(category, AuditCategoryPrefixes))
        {
            return true;
        }

        return IsLogType(document, "audit");
    }

    private static bool IsSecurityLog(JsonObject document)
    {
        var category = document["category"]?.GetValue<string>();
        if (StartsWithAny(category, SecurityCategoryPrefixes))
        {
            return true;
        }

        return IsLogType(document, "security");
    }

    private static bool IsLogType(JsonObject document, string expected)
    {
        var logType = document["logType"]?.GetValue<string>();
        return string.Equals(logType, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPayloadSnapshotDocument(JsonObject document)
    {
        return document["payload"] is not null
               && document["contentType"] is not null
               && document["level"] is null
               && document["category"] is null;
    }

    private static bool StartsWithAny(string? value, IReadOnlyList<string> prefixes)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (string prefix in prefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
        yield return BuildPattern(_options.AuditIndexPrefix);
        yield return BuildPattern(_options.SecurityIndexPrefix);
    }

    private string BuildPattern(string prefix)
        => _options.UseDailyIndexes ? $"{prefix.TrimEnd('-')}-*" : prefix.TrimEnd('-');
}
