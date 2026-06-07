namespace Common.SharedKernel.Logging;

internal sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger,
    ILogContextAccessor contextAccessor,
    IOptions<LoggingMiddlewareOptions> middlewareOptions,
    TimeProvider timeProvider)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        string requestPath = httpContext.Request.Path.Value ?? string.Empty;
        if (ShouldSkip(requestPath, middlewareOptions.Value.ExcludedRoutes))
        {
            await next(httpContext).ConfigureAwait(false);
            return;
        }

        DateTimeOffset startedAtUtc = timeProvider.GetUtcNow();
        Activity? activity = Activity.Current;

        LogContext logContext = BuildLogContext(httpContext, activity);
        using IDisposable scope = contextAccessor.BeginScope(logContext);

        if (middlewareOptions.Value.IncludeRequestStartLog)
        {
            await logger.LogInformationAsync(
                "HTTP request started",
                "http.request.start",
                BuildStartProperties(httpContext, startedAtUtc),
                CancellationToken.None).ConfigureAwait(false);
        }

        long startedTimestamp = Stopwatch.GetTimestamp();
        Exception? capturedException = null;

        try
        {
            await next(httpContext).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            capturedException = exception;
            throw;
        }
        finally
        {
            TimeSpan duration = Stopwatch.GetElapsedTime(startedTimestamp);
            string routeTemplate = ResolveRouteTemplate(httpContext);
            Dictionary<string, object?> completionProperties = new(StringComparer.OrdinalIgnoreCase)
            {
                ["method"] = httpContext.Request.Method,
                ["path"] = requestPath,
                ["routeTemplate"] = routeTemplate,
                ["statusCode"] = httpContext.Response.StatusCode,
                ["durationMs"] = duration.TotalMilliseconds,
                ["traceId"] = activity?.TraceId.ToString(),
                ["spanId"] = activity?.SpanId.ToString()
            };

            if (capturedException is null)
            {
                await logger.LogInformationAsync(
                    "HTTP request completed",
                    "http.request.complete",
                    completionProperties,
                    CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                completionProperties["exceptionType"] = capturedException.GetType().FullName;
                await logger.LogErrorAsync(
                    "HTTP request failed",
                    "http.request.failed",
                    capturedException,
                    completionProperties,
                    CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private static bool ShouldSkip(string path, IReadOnlySet<string> excludedRoutes)
    {
        if (excludedRoutes.Count is 0)
        {
            return false;
        }

        foreach (string route in excludedRoutes)
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                continue;
            }

            if (path.StartsWith(route, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static LogContext BuildLogContext(HttpContext httpContext, Activity? activity)
    {
        string correlationId = ReadHeader(httpContext, Common.SharedKernel.Constants.Headers.CorrelationId)
            ?? activity?.TraceId.ToString()
            ?? httpContext.TraceIdentifier;

        string? tenantId = ReadHeader(httpContext, Common.SharedKernel.Constants.Headers.TenantId);
        string? userId = ReadHeader(httpContext, Common.SharedKernel.Constants.Headers.UserId)
            ?? httpContext.User?.Identity?.Name;

        return new LogContext
        {
            CorrelationId = correlationId,
            RequestId = httpContext.TraceIdentifier,
            TraceId = activity?.TraceId.ToString(),
            SpanId = activity?.SpanId.ToString(),
            TenantId = tenantId,
            UserId = userId
        };
    }

    private static Dictionary<string, object?> BuildStartProperties(HttpContext httpContext, DateTimeOffset startedAtUtc)
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["method"] = httpContext.Request.Method,
            ["path"] = httpContext.Request.Path.Value ?? string.Empty,
            ["scheme"] = httpContext.Request.Scheme,
            ["host"] = httpContext.Request.Host.Value,
            ["startedAtUtc"] = startedAtUtc,
            ["userAgent"] = httpContext.Request.Headers.UserAgent.ToString()
        };
    }

    private static string ResolveRouteTemplate(HttpContext httpContext)
    {
        Endpoint? endpoint = httpContext.GetEndpoint();
        if (endpoint is RouteEndpoint routeEndpoint)
        {
            return routeEndpoint.RoutePattern.RawText ?? httpContext.Request.Path.Value ?? string.Empty;
        }

        return endpoint?.DisplayName ?? httpContext.Request.Path.Value ?? string.Empty;
    }

    private static string? ReadHeader(HttpContext httpContext, string key)
    {
        if (httpContext.Request.Headers.TryGetValue(key, out Microsoft.Extensions.Primitives.StringValues value)
            && !Microsoft.Extensions.Primitives.StringValues.IsNullOrEmpty(value))
        {
            return value.ToString();
        }

        return null;
    }
}
