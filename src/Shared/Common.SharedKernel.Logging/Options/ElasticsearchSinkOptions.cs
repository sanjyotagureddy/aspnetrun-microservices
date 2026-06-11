namespace Common.SharedKernel.Logging;

public sealed record ElasticsearchSinkOptions
{
    public Uri Endpoint { get; set; } = new("http://localhost:9200");

    // Backward-compatible fallback base index name when AppIndexPrefix is not set.
    public string IndexName { get; set; } = "app-log";

    // Base prefix for application logs. Final index uses daily suffix: <prefix>-yyyy.MM.dd.
    public string AppIndexPrefix { get; set; } = "app-log";

    // Base prefix for event logs. Final index uses daily suffix: <prefix>-yyyy.MM.dd.
    public string MessagingIndexPrefix { get; set; } = "messaging-log";

    // Base prefix for audit logs.
    // Final index uses daily suffix: <prefix>-yyyy.MM.dd.
    public string AuditIndexPrefix { get; set; } = "audit-log";

    // Base prefix for security logs.
    // Final index uses daily suffix: <prefix>-yyyy.MM.dd.
    public string SecurityIndexPrefix { get; set; } = "security-log";

    // Base prefix for protected payload documents.
    // Final index uses daily suffix: <prefix>-yyyy.MM.dd.
    public string PayloadIndexPrefix { get; set; } = "payload-log";

    public bool UseDailyIndexes { get; set; } = true;

    public int BatchSize { get; set; } = 1000;

    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(2);

    public int MaxRetryCount { get; set; } = 3;

    public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromMilliseconds(200);

    public string? Username { get; set; }

    public string? Password { get; set; }
}
