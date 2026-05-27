using Common.SharedKernel;
using Common.SharedKernel.Observability.Context;
using Common.SharedKernel.Helpers;

namespace Products.Api.Observability;

internal sealed class AppCallContextMiddleware(RequestDelegate next)
    : AppCallContextMiddlewareBase<AppCallContext>(next, BuildContext)
{
    protected override void ConfigureContext(HttpContext httpContext, AppCallContext context)
    {
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

        var correlationId = GetHeader(httpContext, "X-Correlation-Id") ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers.TryAdd(Constants.Headers.CorrelationId, correlationId);
        var parentCorrelationId = GetHeader(httpContext, "X-Parent-Correlation-Id");
        var traceId = GetHeader(httpContext, "X-Trace-Id");
        var spanId = GetHeader(httpContext, "X-Span-Id");
        var tenantId = GetHeader(httpContext, "X-Tenant-Id");

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
        return httpContext.Request.Headers.TryGetValue(headerName, out var values) ? values.ToString() : null;
    }
}
