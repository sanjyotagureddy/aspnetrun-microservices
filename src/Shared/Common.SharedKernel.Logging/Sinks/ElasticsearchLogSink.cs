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

        string prefix = _options.RouteInfrastructureLogs && IsInfrastructureLog(entry)
            ? infraPrefix
            : apiPrefix;

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
