using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Common.SharedKernel.Helpers;

namespace Common.SharedKernel.Observability.Context;

public static class AppCallContextMiddlewareExtensions
{
    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseAppCallContext(Func<HttpContext, AppCallContextBase> contextFactory)
        {
            Guard.Against.Null(app);
            Guard.Against.Null(contextFactory);
            return app.UseMiddleware<AppCallContextMiddlewareBase<AppCallContextBase>>(contextFactory);
        }

        public IApplicationBuilder UseAppCallContext<TContext>(Func<HttpContext, TContext> contextFactory)
            where TContext : AppCallContextBase
        {
            Guard.Against.Null(app);
            Guard.Against.Null(contextFactory);
            return app.UseMiddleware<AppCallContextMiddlewareBase<TContext>>(contextFactory);
        }

        public IApplicationBuilder UseAppCallContextMiddleware<TMiddleware>()
            where TMiddleware : class
        {
            Guard.Against.Null(app);
            return app.UseMiddleware<TMiddleware>();
        }
    }
}
