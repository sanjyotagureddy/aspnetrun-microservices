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
}
