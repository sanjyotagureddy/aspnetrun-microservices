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
}