namespace Common.SharedKernel.Logging;

internal sealed class ElasticsearchLogSink : ILogSink, IBulkLogSink
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

    private readonly ElasticsearchSinkOptions _options;
    private readonly HttpClient _httpClient;
    private readonly JsonLogFormatter _formatter = new();

    public ElasticsearchLogSink(ElasticsearchSinkOptions options)
    {
        this._options = Guard.Against.Null(options);
        _httpClient = new HttpClient();

        if (string.IsNullOrWhiteSpace(this._options.Username)) return;
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{this._options.Username}:{this._options.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
    }

    public ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
        => WriteBatchAsync([entry], cancellationToken);

    public async ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> entries, CancellationToken cancellationToken = default)
    {
        if (entries.Count is 0)
        {
            return;
        }

        StringBuilder payload = new();
        foreach (LogEntry entry in entries)
        {
            payload.AppendLine($"{{\"index\":{{\"_index\":\"{ResolveIndexName(entry)}\"}}}}");
            payload.AppendLine(_formatter.Format(entry));
        }

        using StringContent content = new(payload.ToString(), Encoding.UTF8, "application/x-ndjson");
        using HttpResponseMessage response = await _httpClient.PostAsync(
            new Uri(_options.Endpoint, "/_bulk"),
            content,
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }

    internal string ResolveIndexName(LogEntry entry)
    {
        string apiPrefix = NormalizePrefix(_options.ApiIndexPrefix, _options.IndexName, "api-logs");
        string infraPrefix = NormalizePrefix(_options.InfraIndexPrefix, "infra-logs", "infra-logs");
        string messagingPrefix = NormalizePrefix(_options.MessagingIndexPrefix, "messaging-log", "messaging-log");
        string auditPrefix = NormalizePrefix(_options.AuditIndexPrefix, "audit-log", "audit-log");
        string securityPrefix = NormalizePrefix(_options.SecurityIndexPrefix, "security-log", "security-log");

        string prefix;
        if (_options.RouteSecurityLogs && IsSecurityLog(entry))
        {
            prefix = securityPrefix;
        }
        else if (_options.RouteAuditLogs && IsAuditLog(entry))
        {
            prefix = auditPrefix;
        }
        else if (_options.RouteMessagingLogs && (IsMessagingLog(entry) || IsEventLog(entry)))
        {
            prefix = messagingPrefix;
        }
        else if (_options.RouteInfrastructureLogs && IsInfrastructureLog(entry))
        {
            prefix = infraPrefix;
        }
        else
        {
            prefix = apiPrefix;
        }

        if (!_options.UseDailyIndexes)
        {
            return prefix;
        }

        string day = entry.TimestampUtc.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);
        return $"{prefix}-{day}";
    }

    private static bool IsInfrastructureLog(LogEntry entry)
    {
        if (InfraNamespacePrefixes.Any(prefix => entry.Namespace.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(entry.Category)
            && InfraCategoryPrefixes.Any(prefix => entry.Category.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (entry.Properties is not null
            && entry.Properties.TryGetValue("logType", out object? logType)
            && string.Equals(logType?.ToString(), "infra", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool IsMessagingLog(LogEntry entry)
    {
        if (MessagingNamespacePrefixes.Any(prefix => entry.Namespace.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(entry.Category)
            && MessagingCategoryPrefixes.Any(prefix => entry.Category.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (entry.Properties is null)
        {
            return false;
        }

        if (entry.Properties.TryGetValue("logType", out object? logType)
            && string.Equals(logType?.ToString(), "messaging", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return entry.Properties.TryGetValue("provider", out object? provider)
               && string.Equals(provider?.ToString(), "Kafka", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEventLog(LogEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Category)
            && EventCategoryPrefixes.Any(prefix => entry.Category.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return HasLogType(entry, "event");
    }

    private static bool IsAuditLog(LogEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Category)
            && AuditCategoryPrefixes.Any(prefix => entry.Category.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return HasLogType(entry, "audit");
    }

    private static bool IsSecurityLog(LogEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Category)
            && SecurityCategoryPrefixes.Any(prefix => entry.Category.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return HasLogType(entry, "security");
    }

    private static bool HasLogType(LogEntry entry, string expected)
    {
        return entry.Properties is not null
               && entry.Properties.TryGetValue("logType", out object? logType)
               && string.Equals(logType?.ToString(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePrefix(string? value, string? fallback, string @default)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim().TrimEnd('-');
        }

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback.Trim().TrimEnd('-');
        }

        return @default;
    }
}
