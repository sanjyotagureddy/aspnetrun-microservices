namespace Common.SharedKernel.Logging;

public sealed record ElasticsearchSinkOptions
{
    public Uri Endpoint { get; set; } = new("http://localhost:9200");

    public string IndexName { get; set; } = "logs";

    public int BatchSize { get; set; } = 1000;

    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(2);

    public int MaxRetryCount { get; set; } = 3;

    public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromMilliseconds(200);

    public string? Username { get; set; }

    public string? Password { get; set; }
}
