namespace Common.SharedKernel.Logging;

internal sealed class LoggingBuilder(IServiceCollection services) : ILoggingBuilder
{
    private readonly LoggingConfiguration _configuration = new();

    public IServiceCollection Services => services;

    public ILoggingBuilder SetServiceName(string serviceName)
    {
        _configuration.Options.ServiceName = Guard.Against.NullOrWhiteSpace(serviceName);
        return this;
    }

    public ILoggingBuilder SetMinimumLevel(LogLevel minimumLevel)
    {
        _configuration.Options.MinimumLevel = minimumLevel;
        return this;
    }

    public ILoggingBuilder AddSink(ILogSink sink)
    {
        _configuration.CustomSinks.Add(Guard.Against.Null(sink));
        return this;
    }

    public ILoggingBuilder UseConsole(Action<ConsoleSinkOptions>? configure = null)
    {
        ConsoleSinkOptions options = new();
        configure?.Invoke(options);
        _configuration.ConsoleSinks.Add(options);
        _configuration.Options.EnabledSinks |= LogSinkKind.Console;
        return this;
    }

    public ILoggingBuilder UseFile(Action<FileSinkOptions>? configure = null)
    {
        FileSinkOptions options = new();
        configure?.Invoke(options);
        _configuration.FileSinks.Add(options);
        _configuration.Options.EnabledSinks |= LogSinkKind.File;
        return this;
    }

    public ILoggingBuilder UseElasticsearch(Action<ElasticsearchSinkOptions>? configure = null)
    {
        ElasticsearchSinkOptions options = new();
        configure?.Invoke(options);
        _configuration.ElasticsearchSinks.Add(options);
        _configuration.Options.EnabledSinks |= LogSinkKind.Elasticsearch;
        return this;
    }

    public ILoggingBuilder AddEnricher(ILogEnricher enricher)
    {
        _configuration.Enrichers.Add(Guard.Against.Null(enricher));
        return this;
    }

    public ILoggingBuilder AddFilter(ILogFilter filter)
    {
        _configuration.Filters.Add(Guard.Against.Null(filter));
        return this;
    }

    public ILoggingBuilder AddCategoryFilter(string categoryPrefix)
    {
        _configuration.Filters.Add(new CategoryPrefixFilter(categoryPrefix));
        return this;
    }

    public ILoggingBuilder AddPropertyFilter(string propertyName, object? expectedValue)
    {
        _configuration.Filters.Add(new PropertyFilter(propertyName, expectedValue));
        return this;
    }

    public void EnsureDefaults()
    {
        if (_configuration.Options.EnabledSinks is LogSinkKind.None)
        {
            UseConsole();
        }

        if (_configuration.Enrichers.Count is 0)
        {
            _configuration.Enrichers.Add(new CorrelationEnricher());
            _configuration.Enrichers.Add(new TraceEnricher());
            _configuration.Enrichers.Add(new EnvironmentEnricher());
            _configuration.Enrichers.Add(new MachineEnricher());
            _configuration.Enrichers.Add(new TenantEnricher());
            _configuration.Enrichers.Add(new UserEnricher());
        }
    }

    public void Register()
    {
        services.AddSingleton(_configuration);
        services.AddSingleton(Options.Create(_configuration.Options));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILogContextAccessor, LogContextAccessor>();
        services.AddSingleton<IReadOnlyList<ILogSink>>(sp => sp.GetRequiredService<LoggingConfiguration>().CreateSinks());
        services.AddSingleton<IReadOnlyList<ILogEnricher>>(sp => sp.GetRequiredService<LoggingConfiguration>().Enrichers.AsReadOnly());
        services.AddSingleton<IReadOnlyList<ILogFilter>>(sp => sp.GetRequiredService<LoggingConfiguration>().Filters.AsReadOnly());
        services.AddSingleton<LogDispatcher>();
        services.AddSingleton<LoggingPipeline>();
        services.AddSingleton<ILoggingFactory, LoggingFactory>();
        services.AddSingleton<IHostedService, LoggingHostedService>();
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
    }
}

internal sealed class LoggingConfiguration
{
    public LoggingOptions Options { get; } = new();

    public List<ILogSink> CustomSinks { get; } = [];

    public List<ConsoleSinkOptions> ConsoleSinks { get; } = [];

    public List<FileSinkOptions> FileSinks { get; } = [];

    public List<ElasticsearchSinkOptions> ElasticsearchSinks { get; } = [];

    public List<ILogEnricher> Enrichers { get; } = [];

    public List<ILogFilter> Filters { get; } = [];

    public IReadOnlyList<ILogSink> CreateSinks()
    {
        List<ILogSink> sinks = [];
        sinks.AddRange(CustomSinks);

        sinks.AddRange(ConsoleSinks.Select(options => new ConsoleLogSink(options)).Cast<ILogSink>());

        sinks.AddRange(FileSinks.Select(options => new FileLogSink(options)).Cast<ILogSink>());

        sinks.AddRange(ElasticsearchSinks.Select(options => new ElasticsearchLogSink(options)).Cast<ILogSink>());

        return sinks;
    }
}
