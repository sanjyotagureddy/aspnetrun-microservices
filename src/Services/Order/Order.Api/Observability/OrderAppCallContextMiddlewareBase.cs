using Common.SharedKernel.Observability.Context;
using Common.SharedKernel.Helpers;

namespace Order.Api.Observability;

internal sealed class OrderAppCallContextMiddlewareBase(RequestDelegate next)
    : AppCallContextMiddlewareBase<OrderAppCallContext>(next, BuildContext)
{
    protected override void ConfigureContext(HttpContext httpContext, OrderAppCallContext context)
    {
        context.Items["serviceName"] = context.ServiceName;

        if (!string.IsNullOrWhiteSpace(context.TenantId))
        {
            context.Items["tenantId"] = context.TenantId;
        }

        context.Items["requestPath"] = httpContext.Request.Path.Value ?? string.Empty;
    }

    private static OrderAppCallContext BuildContext(HttpContext httpContext)
    {
        Guard.Against.Null(httpContext);

        string correlationId = GetHeader(httpContext, "X-Correlation-Id") ?? Guid.NewGuid().ToString("N");
        string? parentCorrelationId = GetHeader(httpContext, "X-Parent-Correlation-Id");
        string? traceId = GetHeader(httpContext, "X-Trace-Id");
        string? spanId = GetHeader(httpContext, "X-Span-Id");
        string? tenantId = GetHeader(httpContext, "X-Tenant-Id");

        return new OrderAppCallContext(
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