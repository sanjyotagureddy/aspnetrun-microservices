using Microsoft.AspNetCore.Http;

namespace Common.SharedKernel.Observability.Context;

public class AppCallContextMiddlewareBase<TContext>(RequestDelegate next, Func<HttpContext, TContext> contextFactory) where TContext : AppCallContextBase
{
    protected virtual TContext CreateContext(HttpContext httpContext)
    {
        return contextFactory(httpContext);
    }

    protected virtual void ConfigureContext(HttpContext httpContext, TContext context)
    {
    }

    public virtual async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        TContext context = CreateContext(httpContext);
        ConfigureContext(httpContext, context);
        using IDisposable _ = AppCallContextBase.BeginScope(context);
        await next(httpContext);
    }
}
