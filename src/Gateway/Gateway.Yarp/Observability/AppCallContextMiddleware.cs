using Common.SharedKernel;
using Common.SharedKernel.Helpers;
using Common.SharedKernel.Observability.Context;
using Microsoft.Extensions.Primitives;

namespace Gateway.Yarp.Observability;

internal sealed class AppCallContextMiddleware(RequestDelegate next)
    : AppCallContextMiddlewareBase<AppCallContext>(next, BuildContext)
{
    protected override void ConfigureContext(HttpContext httpContext, AppCallContext context)
    {
        httpContext.Items["app.call.context"] = context;
        context.Items["serviceName"] = context.ServiceName;

        if (!string.IsNullOrWhiteSpace(context.TenantId))
        {
            context.Items["tenantId"] = context.TenantId;
        }

        context.Items["requestPath"] = httpContext.Request.Path.Value ?? string.Empty;
    }

    private static AppCallContext BuildContext(HttpContext httpContext)
    {
        Guard.Against.Null(httpContext);

        string correlationId = GetHeader(httpContext, Constants.Headers.CorrelationId) ?? Guid.NewGuid().ToString("N");

        // Ensure downstream proxied requests receive correlation headers even when client did not provide one.
        httpContext.Request.Headers[Constants.Headers.CorrelationId] = correlationId;
        httpContext.Response.Headers.TryAdd(Constants.Headers.CorrelationId, correlationId);

        string? parentCorrelationId = GetHeader(httpContext, Constants.Headers.ParentCorrelationId);
        string? traceId = GetHeader(httpContext, Constants.Headers.TraceId);
        string? spanId = GetHeader(httpContext, Constants.Headers.SpanId);
        string? tenantId = GetHeader(httpContext, Constants.Headers.TenantId);

        return new AppCallContext(
            correlationId,
            parentCorrelationId,
            traceId,
            spanId,
            tenantId,
            headers: httpContext.Request.Headers.ToDictionary(header => header.Key, header => header.Value.ToString(), StringComparer.OrdinalIgnoreCase),
            items: new Dictionary<string, object?>
            {
                ["method"] = httpContext.Request.Method
            });
    }

    private static string? GetHeader(HttpContext httpContext, string headerName)
    {
        return httpContext.Request.Headers.TryGetValue(headerName, out StringValues values)
            && !StringValues.IsNullOrEmpty(values)
            ? values.ToString()
            : null;
    }
}
