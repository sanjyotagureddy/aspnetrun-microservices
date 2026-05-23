using System.Diagnostics.CodeAnalysis;
using SharedKernel.Context;
using SharedKernel.Middleware;

namespace Shopping.Aggregator.Middlewares;

/// <summary>
/// Shopping aggregator request context middleware.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RequestContextMiddleware(RequestDelegate next, IHostEnvironment environment, IEnumerable<IRequestContextEnricher> enrichers)
    : RequestContextMiddlewareBase(next, environment, enrichers)
{
    protected override RequestContext CreateRequestContext(HttpContext httpContext, string serviceName)
    {
        return AppContext.FromHttpContext(httpContext, serviceName);
    }
}
