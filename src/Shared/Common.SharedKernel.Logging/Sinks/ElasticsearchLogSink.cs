namespace Common.SharedKernel.Logging;

internal sealed class ElasticsearchLogSink : ILogSink, IBulkLogSink
{
    private readonly ElasticsearchSinkOptions _options;
    private readonly HttpClient _httpClient;
    private readonly JsonLogFormatter _formatter = new();

    public ElasticsearchLogSink(ElasticsearchSinkOptions options)
    {
        this._options = Guard.Against.Null(options);
        _httpClient = new HttpClient();

        if (!string.IsNullOrWhiteSpace(this._options.Username))
        {
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{this._options.Username}:{this._options.Password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        }
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
            payload.AppendLine("{\"index\":{}}");
            payload.AppendLine(_formatter.Format(entry));
        }

        using StringContent content = new(payload.ToString(), Encoding.UTF8, "application/x-ndjson");
        using HttpResponseMessage response = await _httpClient.PostAsync(
            new Uri(_options.Endpoint, $"/{_options.IndexName}/_bulk"),
            content,
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }
}
