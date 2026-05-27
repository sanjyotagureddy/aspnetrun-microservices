using Common.SharedKernel.Helpers;

namespace Common.SharedKernel.Observability.Context;

public abstract class AppCallContextBase(
    string correlationId,
    string? parentCorrelationId = null,
    string? traceId = null,
    string? spanId = null,
    IDictionary<string, string>? headers = null,
    IDictionary<string, object?>? items = null)
{
    private static readonly AsyncLocal<AppCallContextBase?> SCurrent = new();

    public static AppCallContextBase? Current => SCurrent.Value;

    public string ApplicationName { get; } = Guard.Against.NullOrWhiteSpace(correlationId);
    public string CorrelationId { get; } = Guard.Against.NullOrWhiteSpace(correlationId);

    public string? ParentCorrelationId { get; } = string.IsNullOrWhiteSpace(parentCorrelationId)
        ? null
        : parentCorrelationId;

    public string? TraceId { get; } = string.IsNullOrWhiteSpace(traceId) ? null : traceId;

    public string? SpanId { get; } = string.IsNullOrWhiteSpace(spanId) ? null : spanId;

    public IDictionary<string, string> Headers { get; } = headers is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, object?> Items { get; } = items is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(items);

    public static TContext? CurrentAs<TContext>() where TContext : AppCallContextBase
    {
        return SCurrent.Value as TContext;
    }

    public static IDisposable BeginScope(AppCallContextBase context)
    {
        Guard.Against.Null(context);

        var previous = SCurrent.Value;
        SCurrent.Value = context;
        return new AmbientScope(previous);
    }

    private sealed class AmbientScope(AppCallContextBase? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            SCurrent.Value = previous;
            _disposed = true;
        }
    }
}
