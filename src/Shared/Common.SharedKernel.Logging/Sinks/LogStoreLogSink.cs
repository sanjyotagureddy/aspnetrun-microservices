using System.Text.Json.Nodes;
using Common.SharedKernel;

namespace Common.SharedKernel.Logging;

internal sealed class LogStoreLogSink(LogStoreSinkOptions options) : ILogSink, IBulkLogSink
{
    private readonly LogStoreSinkOptions _options = Guard.Against.Null(options);
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly JsonLogFormatter _formatter = new();
    private readonly Uri _resolvedEndpoint = LogStoreEndpointResolver.Resolve(options.Endpoint);

    public ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
        => WriteBatchAsync([entry], cancellationToken);

    public async ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> entries, CancellationToken cancellationToken = default)
    {
        if (entries.Count is 0)
        {
            return;
        }

        Uri route = new(_resolvedEndpoint, _options.CreateRoutePath);

        foreach (LogEntry entry in entries)
        {
            var document = JsonNode.Parse(_formatter.Format(entry));
            JsonObject payload = new()
            {
                ["document"] = document
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, route)
            {
                Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
            };

            AddHeader(request, Constants.Headers.CorrelationId, entry.CorrelationId);

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
    }

    private static void AddHeader(HttpRequestMessage request, string headerName, string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return;
        }

        request.Headers.Remove(headerName);
        request.Headers.TryAddWithoutValidation(headerName, headerValue);
    }
}
