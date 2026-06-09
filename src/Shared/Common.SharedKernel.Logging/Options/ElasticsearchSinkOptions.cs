namespace Common.SharedKernel.Logging;

public sealed record ElasticsearchSinkOptions
{
    public Uri Endpoint { get; set; } = new("http://localhost:9200");

    // Backward-compatible fallback base index name when ApiIndexPrefix is not set.
    public string IndexName { get; set; } = "api-logs";

    // Base prefix for business/API logs. Final index uses daily suffix: <prefix>-yyyy.MM.dd.
    public string ApiIndexPrefix { get; set; } = "api-logs";

    // Base prefix for protected payload documents. Final index uses daily suffix: <prefix>-yyyy.MM.dd.
    public string PayloadIndexPrefix { get; set; } = "api-payload";

    // Base prefix for platform/framework/infra logs. Final index uses daily suffix: <prefix>-yyyy.MM.dd.
    public string InfraIndexPrefix { get; set; } = "infra-logs";

    public bool RouteInfrastructureLogs { get; set; } = true;

    // Base prefix for messaging and event logs (Kafka, consumer/producer pipeline, domain/business events).
    // Final index uses daily suffix: <prefix>-yyyy.MM.dd.
    public string MessagingIndexPrefix { get; set; } = "messaging-log";

    public bool RouteMessagingLogs { get; set; } = true;

    // Base prefix for audit logs.
    // Final index uses daily suffix: <prefix>-yyyy.MM.dd.
    public string AuditIndexPrefix { get; set; } = "audit-log";

    public bool RouteAuditLogs { get; set; } = true;

    // Base prefix for security logs.
    // Final index uses daily suffix: <prefix>-yyyy.MM.dd.
    public string SecurityIndexPrefix { get; set; } = "security-log";

    public bool RouteSecurityLogs { get; set; } = true;

    public bool UseDailyIndexes { get; set; } = true;

    public int BatchSize { get; set; } = 1000;

    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(2);

    public int MaxRetryCount { get; set; } = 3;

    public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromMilliseconds(200);

    public string? Username { get; set; }

    public string? Password { get; set; }
}
