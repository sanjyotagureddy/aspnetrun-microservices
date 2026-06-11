namespace Common.SharedKernel.Logging;

public abstract record BaseLog
{
    public string Message { get; init; } = string.Empty;

    public string? Category { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, object?>? Context { get; init; }
}

public sealed record TraceLog : BaseLog
{
    public string? Operation { get; init; }

    public double? DurationMs { get; init; }
}

public sealed record ApiLog : BaseLog
{
    public string? Method { get; init; }

    public string? Path { get; init; }

    public string? RouteTemplate { get; init; }

    public string? Url { get; init; }

    public int? StatusCode { get; init; }

    public double? DurationMs { get; init; }

    public IReadOnlyDictionary<string, object?>? RequestHeaders { get; init; }

    public IReadOnlyDictionary<string, object?>? ResponseHeaders { get; init; }

    public string? RequestPayloadRef { get; init; }

    public string? ResponsePayloadRef { get; init; }
}

public sealed record ErrorLog : BaseLog
{
    public Exception? Exception { get; init; }

    public string? ExceptionType { get; init; }

    public string? ExceptionMessage { get; init; }

    public string? ErrorCode { get; init; }
}