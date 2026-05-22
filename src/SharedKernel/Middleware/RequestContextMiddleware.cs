using Microsoft.Extensions.Hosting;
using SharedKernel.Context;

namespace SharedKernel.Middleware;

public sealed class RequestContextMiddleware(RequestDelegate next, IHostEnvironment environment, IEnumerable<IRequestContextEnricher> enrichers)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        var appContext = RequestContext.FromHttpContext(httpContext, environment.ApplicationName);

        foreach (IRequestContextEnricher enricher in enrichers)
        {
            enricher.EnrichRequest(httpContext, appContext);
        }

        using (RequestContextScope.BeginScope(appContext))
        {
            httpContext.Response.OnStarting(() =>
            {
                foreach (IRequestContextEnricher enricher in enrichers)
                {
                    enricher.EnrichResponse(httpContext, appContext);
                }

                return Task.CompletedTask;
            });

            await next(httpContext);
        }
    }
}
