namespace Common.SharedKernel.Logging;

internal sealed class ElasticsearchLogSink : ILogSink, IBulkLogSink
{
    private readonly ElasticsearchSinkOptions _options;
    private readonly HttpClient _httpClient;
    private readonly JsonLogFormatter _formatter = new();

    public ElasticsearchLogSink(ElasticsearchSinkOptions options)
    {
        _options = Guard.Against.Null(options);
        _httpClient = new HttpClient();

        if (string.IsNullOrWhiteSpace(_options.Username))
        {
            return;
        }

        string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));
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
        string appPrefix = NormalizePrefix(_options.AppIndexPrefix, _options.IndexName, "app-log");
        string eventPrefix = NormalizePrefix(_options.MessagingIndexPrefix, "messaging-log", "messaging-log");
        string auditPrefix = NormalizePrefix(_options.AuditIndexPrefix, "audit-log", "audit-log");
        string securityPrefix = NormalizePrefix(_options.SecurityIndexPrefix, "security-log", "security-log");

        string destination = ResolveDestination(entry);
        string prefix = destination switch
        {
            "event" => eventPrefix,
            "audit" => auditPrefix,
            "security" => securityPrefix,
            _ => appPrefix
        };

        if (!_options.UseDailyIndexes)
        {
            return prefix;
        }

        string day = entry.TimestampUtc.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);
        return $"{prefix}-{day}";
    }

    private static string ResolveDestination(LogEntry entry)
    {
        if (entry.Properties is null
            || !entry.Properties.TryGetValue("logType", out object? value)
            || string.IsNullOrWhiteSpace(value?.ToString()))
        {
            return "app";
        }

        string normalized = value.ToString()!.Trim().ToLowerInvariant();
        return normalized switch
        {
            "app" => "app",
            "application" => "app",
            "event" => "event",
            "audit" => "audit",
            "security" => "security",
            _ => "app"
        };
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
