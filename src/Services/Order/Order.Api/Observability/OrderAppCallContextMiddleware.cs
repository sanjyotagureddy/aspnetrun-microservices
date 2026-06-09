using Common.SharedKernel;
using Common.SharedKernel.Helpers;
using Common.SharedKernel.Observability.Context;
using Microsoft.Extensions.Primitives;

namespace Order.Api.Observability;

internal sealed class OrderAppCallContextMiddleware(RequestDelegate next)
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

        string correlationId = GetHeader(httpContext, Constants.Headers.CorrelationId) ?? Guid.NewGuid().ToString("N");
        httpContext.Response.Headers.TryAdd(Constants.Headers.CorrelationId, correlationId);

        string? parentCorrelationId = GetHeader(httpContext, Constants.Headers.ParentCorrelationId);
        string? traceId = GetHeader(httpContext, Constants.Headers.TraceId);
        string? spanId = GetHeader(httpContext, Constants.Headers.SpanId);
        string? tenantId = GetHeader(httpContext, Constants.Headers.TenantId);

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
        return httpContext.Request.Headers.TryGetValue(headerName, out StringValues values)
            && !StringValues.IsNullOrEmpty(values)
            ? values.ToString()
            : null;
    }
}
