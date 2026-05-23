using Microsoft.Extensions.Hosting;
using SharedKernel.Context;

namespace SharedKernel.Middleware;

/// <summary>
/// Base middleware that creates and scopes the current request context.
/// </summary>
public abstract class RequestContextMiddlewareBase(RequestDelegate next, IHostEnvironment environment, IEnumerable<IRequestContextEnricher> enrichers)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly IHostEnvironment _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    private readonly IEnumerable<IRequestContextEnricher> _enrichers = enrichers ?? throw new ArgumentNullException(nameof(enrichers));

    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        RequestContext requestContext = CreateRequestContext(httpContext, _environment.ApplicationName);

        foreach (IRequestContextEnricher enricher in _enrichers)
        {
            enricher.EnrichRequest(httpContext, requestContext);
        }

        using (RequestContextScope.BeginScope(requestContext))
        {
            httpContext.Response.OnStarting(() =>
            {
                foreach (IRequestContextEnricher enricher in _enrichers)
                {
                    enricher.EnrichResponse(httpContext, requestContext);
                }

                return Task.CompletedTask;
            });

            await _next(httpContext);
        }
    }

    protected abstract RequestContext CreateRequestContext(HttpContext httpContext, string serviceName);
}