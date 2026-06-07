namespace Common.SharedKernel.Logging;

public sealed record LoggingMiddlewareOptions
{
    public HashSet<string> ExcludedRoutes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/metrics",
        "/swagger"
    };

    public bool IncludeRequestStartLog { get; set; } = true;

    public bool CaptureRequestPayloadCandidates { get; set; } = true;

    public bool CaptureResponsePayloadCandidates { get; set; } = true;

    public bool CaptureExceptionPayloadCandidates { get; set; } = true;

    public int MaxPayloadCaptureBytes { get; set; } = 16 * 1024;

    public HashSet<string> AllowedPayloadContentTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
        "application/problem+json",
        "text/plain"
    };

    public HashSet<string> ExcludedPayloadRoutes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/metrics",
        "/swagger"
    };
}