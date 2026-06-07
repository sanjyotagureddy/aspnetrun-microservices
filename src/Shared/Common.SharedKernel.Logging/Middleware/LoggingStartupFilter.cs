namespace Common.SharedKernel.Logging;

internal sealed class LoggingStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<RequestLoggingMiddleware>();
            next(app);
        };
    }
}