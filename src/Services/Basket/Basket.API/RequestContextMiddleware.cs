using SharedKernel.Context;
using SharedKernel.Middleware;

namespace Basket.API;

/// <summary>
/// Basket API request context middleware.
/// </summary>
public sealed class RequestContextMiddleware(RequestDelegate next, IHostEnvironment environment, IEnumerable<IRequestContextEnricher> enrichers)
    : RequestContextMiddlewareBase(next, environment, enrichers)
{
    protected override RequestContext CreateRequestContext(HttpContext httpContext, string serviceName)
    {
        return AppContext.FromHttpContext(httpContext, serviceName);
    }
}