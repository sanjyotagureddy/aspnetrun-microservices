namespace Common.SharedKernel.Logging;

public static class LoggingServiceCollectionExtensions
{
    public static IServiceCollection AddCommonSharedKernelLogging(
        this IServiceCollection services,
        Action<ILoggingBuilder> configure)
    {
        Guard.Against.Null(services);
        Guard.Against.Null(configure);

        LoggingBuilder builder = new(services);
        configure(builder);
        builder.EnsureDefaults();
        builder.Register();
        return services;
    }

    public static IServiceCollection UseRequestLoggingMiddleware<TMiddleware>(this IServiceCollection services)
        where TMiddleware : RequestLoggingMiddlewareBase
    {
        Guard.Against.Null(services);

        services.AddSingleton(new RequestLoggingMiddlewareRegistration(typeof(TMiddleware)));
        return services;
    }
}
