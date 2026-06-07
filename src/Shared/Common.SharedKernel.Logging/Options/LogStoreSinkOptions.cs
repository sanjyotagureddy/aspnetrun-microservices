namespace Common.SharedKernel.Logging;

public sealed record LogStoreSinkOptions
{
    public Uri Endpoint { get; set; } = new("http://localhost:9200");

    public string CreateRoutePath { get; set; } = "/api/v1/logs";

    public bool EnablePayloadDeduplication { get; set; } = true;

    public int MaxPayloadDedupEntries { get; set; } = 10000;
}
