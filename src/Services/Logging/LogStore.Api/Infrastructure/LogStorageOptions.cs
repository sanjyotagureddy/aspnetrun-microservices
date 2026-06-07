namespace LogStore.Api.Infrastructure;

public sealed record LogStorageOptions
{
    public Uri Endpoint { get; set; } = new("http://localhost:9200");

    public string ApiIndexPrefix { get; set; } = "api-logs";

    public string PayloadIndexPrefix { get; set; } = "api-payload";

    public string InfraIndexPrefix { get; set; } = "infra-logs";

    public string MessagingIndexPrefix { get; set; } = "messaging-log";

    public string AuditIndexPrefix { get; set; } = "audit-log";

    public string SecurityIndexPrefix { get; set; } = "security-log";

    public bool UseDailyIndexes { get; set; } = true;
}
