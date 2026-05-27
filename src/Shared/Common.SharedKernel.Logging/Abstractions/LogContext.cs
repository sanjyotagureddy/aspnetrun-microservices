namespace Common.SharedKernel.Logging;

public sealed record LogContext
{
    public string? CorrelationId { get; init; }

    public string? RequestId { get; init; }

    public string? TraceId { get; init; }

    public string? SpanId { get; init; }

    public string? TenantId { get; init; }

    public string? UserId { get; init; }

    public IReadOnlyDictionary<string, object?>? Properties { get; init; }
}
