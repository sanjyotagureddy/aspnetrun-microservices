namespace Common.SharedKernel.Logging;

internal sealed class LoggingStartupFilter(RequestLoggingMiddlewareRegistration registration) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware(registration.MiddlewareType);
            next(app);
        };
    }
}