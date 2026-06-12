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

    public ILoggingBuilder SetEnabledLogTypes(IEnumerable<string> enabledLogTypes)
    {
        if (enabledLogTypes is null)
        {
            throw new ArgumentNullException(nameof(enabledLogTypes));
        }

        HashSet<string> normalized = new(StringComparer.OrdinalIgnoreCase);
        foreach (string candidate in enabledLogTypes)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            normalized.Add(candidate.Trim());
        }

        if (normalized.Count > 0)
        {
            _configuration.Options.EnabledLogTypes = normalized;
        }

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

    public ILoggingBuilder AddMasker(IMask masker)
    {
        _configuration.Maskers.Add(Guard.Against.Null(masker));
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
        if (_configuration.CustomSinks.Count is 0
            && _configuration.ConsoleSinks.Count is 0
            && _configuration.FileSinks.Count is 0
            && _configuration.ElasticsearchSinks.Count is 0)
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

        if (_configuration.Maskers.Count is 0)
        {
            _configuration.Maskers.Add(new CreditCardMask());
            _configuration.Maskers.Add(new PhoneMask());
            _configuration.Maskers.Add(new EmailMask());
            _configuration.Maskers.Add(new TokenMask());
            _configuration.Maskers.Add(new DefaultMask());
        }
    }

    public void Register()
    {
        _configuration.Validate();

        services.AddSingleton(_configuration);
        services.AddSingleton(Options.Create(_configuration.Options));
        services.AddSingleton<IReadOnlyList<IMask>>(sp => sp.GetRequiredService<LoggingConfiguration>().Maskers.AsReadOnly());
        OptionsBuilder<MaskingOptions> maskingOptionsBuilder = services.AddOptions<MaskingOptions>();
        if (services.Any(descriptor => descriptor.ServiceType == typeof(Microsoft.Extensions.Configuration.IConfiguration)))
        {
            maskingOptionsBuilder.BindConfiguration("Logging:Masking");
        }

        OptionsBuilder<LoggingPolicyOptions> policyOptionsBuilder = services.AddOptions<LoggingPolicyOptions>();
        if (services.Any(descriptor => descriptor.ServiceType == typeof(Microsoft.Extensions.Configuration.IConfiguration)))
        {
            policyOptionsBuilder.BindConfiguration("Logging:Policy");
        }

        policyOptionsBuilder.PostConfigure(policy => policy.EnsureDefaults());
        OptionsBuilder<PayloadProtectionOptions> payloadProtectionOptionsBuilder = services.AddOptions<PayloadProtectionOptions>();
        if (services.Any(descriptor => descriptor.ServiceType == typeof(Microsoft.Extensions.Configuration.IConfiguration)))
        {
            payloadProtectionOptionsBuilder.BindConfiguration("Logging:PayloadProtection");
        }

        payloadProtectionOptionsBuilder.PostConfigure(policy => policy.EnsureDefaults());
        OptionsBuilder<LoggingMiddlewareOptions> middlewareOptionsBuilder = services.AddOptions<LoggingMiddlewareOptions>();
        if (services.Any(descriptor => descriptor.ServiceType == typeof(Microsoft.Extensions.Configuration.IConfiguration)))
        {
            middlewareOptionsBuilder.BindConfiguration("Logging:Middleware");
        }

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILogContextAccessor, LogContextAccessor>();
        services.AddSingleton<ILogRedactor, DefaultLogRedactor>();
        services.AddSingleton<IPayloadMaskingEngine, DefaultPayloadMaskingEngine>();
        services.AddSingleton<IPayloadProtectionPipeline, PayloadProtectionPipeline>();
        services.AddSingleton<IPayloadStore>(sp =>
        {
            LoggingConfiguration configuration = sp.GetRequiredService<LoggingConfiguration>();

            ElasticsearchSinkOptions? elasticsearchOptions = configuration.ElasticsearchSinks.FirstOrDefault();
            if (elasticsearchOptions is not null)
            {
                return new ElasticsearchPayloadStore(elasticsearchOptions);
            }

            return new NoopPayloadStore();
        });

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(RequestLoggingMiddlewareRegistration)))
        {
            services.AddSingleton(new RequestLoggingMiddlewareRegistration(typeof(RequestLoggingMiddleware)));
        }

        services.AddTransient<IStartupFilter, LoggingStartupFilter>();
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

    public List<IMask> Maskers { get; } = [];

    public IReadOnlyList<ILogSink> CreateSinks()
    {
        List<ILogSink> sinks = [];
        sinks.AddRange(CustomSinks);

        sinks.AddRange(ConsoleSinks.Select(options => (ILogSink)new ConsoleLogSink(options)));

        sinks.AddRange(FileSinks.Select(options => (ILogSink)new FileLogSink(options)));

        sinks.AddRange(ElasticsearchSinks.Select(options => (ILogSink)new ElasticsearchLogSink(options)));

        return sinks;
    }

    public void Validate()
    {
        if (Options.BatchSize <= 0)
        {
            throw new InvalidOperationException("Logging options must define a BatchSize greater than zero.");
        }

        if (Options.QueueCapacity <= 0)
        {
            throw new InvalidOperationException("Logging options must define a QueueCapacity greater than zero.");
        }

        foreach (ConsoleSinkOptions options in ConsoleSinks)
        {
            if (!Enum.IsDefined(options.FormatterKind))
            {
                throw new InvalidOperationException("Console sink formatter kind is invalid.");
            }
        }

        foreach (FileSinkOptions options in FileSinks)
        {
            if (string.IsNullOrWhiteSpace(options.FilePath))
            {
                throw new InvalidOperationException("File sink FilePath is required.");
            }

            if (!Enum.IsDefined(options.FormatterKind))
            {
                throw new InvalidOperationException("File sink formatter kind is invalid.");
            }
        }

        foreach (ElasticsearchSinkOptions options in ElasticsearchSinks)
        {
            if (options.Endpoint is null || !options.Endpoint.IsAbsoluteUri)
            {
                throw new InvalidOperationException("Elasticsearch sink endpoint must be an absolute URI.");
            }

            if (string.IsNullOrWhiteSpace(options.IndexName))
            {
                throw new InvalidOperationException("Elasticsearch sink index name is required.");
            }
        }
    }
}
