using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Common.SharedKernel.Observability.Context;

public static class AppCallContextMiddlewareExtensions
{
    public static IApplicationBuilder UseAppCallContext(
        this IApplicationBuilder app,
        Func<HttpContext, AppCallContextBase> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(contextFactory);
        return app.UseMiddleware<AppCallContextMiddleware<AppCallContextBase>>(contextFactory);
    }

    public static IApplicationBuilder UseAppCallContext<TContext>(
        this IApplicationBuilder app,
        Func<HttpContext, TContext> contextFactory)
        where TContext : AppCallContextBase
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(contextFactory);
        return app.UseMiddleware<AppCallContextMiddleware<TContext>>(contextFactory);
    }

    public static IApplicationBuilder UseAppCallContextMiddleware<TMiddleware>(this IApplicationBuilder app)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<TMiddleware>();
    }
}
