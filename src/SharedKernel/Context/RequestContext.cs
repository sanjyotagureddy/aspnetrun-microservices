using System.Diagnostics;
using Microsoft.Extensions.Primitives;

namespace SharedKernel.Context;

public class RequestContext(string serviceName)
{
    public static RequestContext Current => RequestContextScope.Current;

    public string ServiceName { get; } = serviceName ?? throw new ArgumentNullException(nameof(serviceName));

    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");

    public string TransactionId { get; init; } = Guid.NewGuid().ToString("N");

    public string RequestId { get; init; } = Guid.NewGuid().ToString("N");

    public string TraceId { get; init; } = Guid.NewGuid().ToString("N");

    public string? TenantId { get; init; }

    public string? SessionId { get; init; }

    public DateTimeOffset RequestStartTimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public static RequestContext FromHttpContext(HttpContext httpContext, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        IHeaderDictionary headers = httpContext.Request.Headers;

        var safeHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AddHeaderIfPresent(safeHeaders, headers, Constants.Headers.CorrelationId);
        AddHeaderIfPresent(safeHeaders, headers, Constants.Headers.TransactionId);
        AddHeaderIfPresent(safeHeaders, headers, Constants.Headers.SessionId);
        AddHeaderIfPresent(safeHeaders, headers, Constants.Headers.TenantId);

        var cId = GetHeaderOrFallback(headers, Constants.Headers.CorrelationId);
        httpContext.Response.Headers.TryAdd(Constants.Headers.CorrelationId, cId);
        //httpContext.Response.Headers.TryAdd(Constants.Headers.TraceId, Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier);

        return new RequestContext(serviceName)
        {
            CorrelationId = cId,
            TransactionId = GetHeaderOrFallback(headers, Constants.Headers.TransactionId),
            RequestId = httpContext.TraceIdentifier,
            TraceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier,
            SessionId = GetHeaderOrNull(headers, Constants.Headers.SessionId),
            TenantId = GetHeaderOrNull(headers, Constants.Headers.TenantId),
            RequestStartTimestampUtc = DateTimeOffset.UtcNow,
            Headers = safeHeaders
        };
    }

    private static void AddHeaderIfPresent(
        IDictionary<string, string> target,
        IHeaderDictionary source,
        string key)
    {
        if (source.TryGetValue(key, out StringValues value) && !string.IsNullOrWhiteSpace(value.ToString()))
        {
            target[key] = value.ToString();
        }
    }

    private static string GetHeaderOrFallback(IHeaderDictionary headers, string key)
    {
        return headers.TryGetValue(key, out StringValues value) && !string.IsNullOrWhiteSpace(value.ToString())
            ? value.ToString()
            : Guid.NewGuid().ToString();
    }

    private static string? GetHeaderOrNull(IHeaderDictionary headers, string key)
    {
        return headers.TryGetValue(key, out StringValues value) && !string.IsNullOrWhiteSpace(value.ToString())
            ? value.ToString()
            : null;
    }
}
