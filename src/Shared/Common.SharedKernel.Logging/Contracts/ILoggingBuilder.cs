namespace Common.SharedKernel.Logging;

public interface ILoggingBuilder
{
    IServiceCollection Services { get; }

    ILoggingBuilder SetServiceName(string serviceName);

    ILoggingBuilder SetMinimumLevel(LogLevel minimumLevel);

    ILoggingBuilder AddSink(ILogSink sink);

    ILoggingBuilder UseConsole(Action<ConsoleSinkOptions>? configure = null);

    ILoggingBuilder UseFile(Action<FileSinkOptions>? configure = null);

    ILoggingBuilder UseElasticsearch(Action<ElasticsearchSinkOptions>? configure = null);

    ILoggingBuilder AddEnricher(ILogEnricher enricher);

    ILoggingBuilder AddFilter(ILogFilter filter);

    ILoggingBuilder AddCategoryFilter(string categoryPrefix);

    ILoggingBuilder AddPropertyFilter(string propertyName, object? expectedValue);
}
