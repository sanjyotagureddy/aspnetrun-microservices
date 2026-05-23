using SharedKernel.Context;
using SharedKernel.Middleware;

namespace Discount.Grpc;

/// <summary>
/// Discount gRPC request context middleware.
/// </summary>
public sealed class RequestContextMiddleware(RequestDelegate next, IHostEnvironment environment, IEnumerable<IRequestContextEnricher> enrichers)
    : RequestContextMiddlewareBase(next, environment, enrichers)
{
    protected override RequestContext CreateRequestContext(HttpContext httpContext, string serviceName)
    {
        return AppContext.FromHttpContext(httpContext, serviceName);
    }
}