using Microsoft.AspNetCore.Http;

namespace Common.SharedKernel.Observability.Correlation;

public sealed class AppCallContextMiddleware<TContext>(
    RequestDelegate next,
    Func<HttpContext, TContext> contextFactory)
    where TContext : AppCallContextBase
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        TContext context = contextFactory(httpContext);
        using IDisposable _ = AppCallContextBase.BeginScope(context);
        await next(httpContext);
    }
}