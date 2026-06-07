using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Common.SharedKernel.Logging.UnitTests;

public sealed class AdditionalCoverageTests
{
    [Fact]
    public void AddCommonSharedKernelLogging_ShouldApplyDefaults_WhenNoSinksOrEnrichersConfigured()
    {
        ServiceCollection services = new();
        services.AddCommonSharedKernelLogging(builder => builder.SetServiceName("Catalog.Api"));

        using ServiceProvider provider = services.BuildServiceProvider();

        IReadOnlyList<ILogSink> sinks = provider.GetRequiredService<IReadOnlyList<ILogSink>>();
        IReadOnlyList<ILogEnricher> enrichers = provider.GetRequiredService<IReadOnlyList<ILogEnricher>>();

        sinks.Should().ContainSingle(s => s is ConsoleLogSink);
        enrichers.Should().HaveCount(6);
        provider.GetRequiredService<ILoggingFactory>().Should().NotBeNull();
        provider.GetRequiredService<ILogger<AdditionalCoverageTests>>().Should().NotBeNull();
    }

    [Fact]
    public void AddCommonSharedKernelLogging_ShouldSupportAllBuilderConfigurationMethods()
    {
        string filePath = Path.Combine(Path.GetTempPath(), "logging-tests", Guid.NewGuid().ToString("N"), "combined.log");
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddCommonSharedKernelLogging(builder =>
        {
            builder.SetServiceName("Catalog.Api");
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddSink(new NoopSink());
            builder.UseConsole(o => o.FormatterKind = LogFormatterKind.Text);
            builder.UseFile(o =>
            {
                o.FilePath = filePath;
                o.FormatterKind = LogFormatterKind.Json;
            });
            builder.UseElasticsearch(o =>
            {
                o.Endpoint = new Uri("http://localhost:9200");
                o.IndexName = "logs";
            });
            builder.AddEnricher(new CorrelationEnricher());
            builder.AddFilter(new MinimumLevelFilter(LogLevel.Debug));
            builder.AddCategoryFilter("cat");
            builder.AddPropertyFilter("k", "v");
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IReadOnlyList<ILogSink>>().Should().HaveCountGreaterThanOrEqualTo(4);
        provider.GetRequiredService<IReadOnlyList<ILogFilter>>().Should().HaveCount(3);
    }

    [Fact]
    public void AddCommonSharedKernelLogging_ShouldThrow_WhenFormatterKindIsInvalid()
    {
        ServiceCollection services = new();

        Action action = () => services.AddCommonSharedKernelLogging(builder =>
        {
            builder.SetServiceName("Catalog.Api");
            builder.UseConsole(o => o.FormatterKind = (LogFormatterKind)999);
        });

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Logger_NonGenericOverloads_ShouldDispatch()
    {
        CollectSink sink = new();
        LogDispatcher dispatcher = new([sink], Options.Create(new LoggingOptions
        {
            ServiceName = "Catalog",
            MinimumLevel = LogLevel.Trace,
            BatchSize = 1,
            QueueCapacity = 32
        }));

        LoggingPipeline pipeline = new(
            new LogContextAccessor(),
            Options.Create(new LoggingOptions
            {
                ServiceName = "Catalog",
                MinimumLevel = LogLevel.Trace,
                BatchSize = 1,
                QueueCapacity = 32
            }),
            [],
            [],
            new DefaultLogRedactor(Options.Create(new LoggingPolicyOptions())),
            dispatcher,
            TimeProvider.System);

        Logger logger = new(pipeline, "Catalog.Api");
        using CancellationTokenSource cts = new();
        Task loop = dispatcher.RunAsync(cts.Token);
        CancellationToken token = TestContext.Current.CancellationToken;

        await logger.LogInformationAsync("m1", category: null, properties: null, cancellationToken: token);
        await logger.LogInformationAsync("m2", "cat", cancellationToken: token);
        await logger.LogErrorAsync(new Exception("e"), cancellationToken: token);
        await logger.LogCriticalAsync(new Exception("f"), cancellationToken: token);

        await sink.WaitForCountAsync(4, TimeSpan.FromSeconds(5));

        cts.Cancel();
        await loop;

        sink.Entries.Should().Contain(e => e.Message == "m1");
        sink.Entries.Should().Contain(e => e.Category == "cat");
        sink.Entries.Should().Contain(e => e.Category == "exception");
        sink.Entries.Should().Contain(e => e.Category == "fatal");
    }

    [Fact]
    public async Task LoggingPipeline_ShouldCaptureActivityAndApplyRedaction()
    {
        CollectSink sink = new();
        LogDispatcher dispatcher = new([sink], Options.Create(new LoggingOptions
        {
            ServiceName = "Catalog",
            MinimumLevel = LogLevel.Trace,
            BatchSize = 1,
            QueueCapacity = 32,
            CaptureActivityContext = true
        }));

        LoggingPipeline pipeline = new(
            new LogContextAccessor(),
            Options.Create(new LoggingOptions
            {
                ServiceName = "Catalog",
                MinimumLevel = LogLevel.Trace,
                BatchSize = 1,
                QueueCapacity = 32,
                CaptureActivityContext = true
            }),
            [new CorrelationEnricher(), new TraceEnricher(), new TenantEnricher(), new UserEnricher()],
            [],
            new DefaultLogRedactor(Options.Create(new LoggingPolicyOptions())),
            dispatcher,
            TimeProvider.System);

        using CancellationTokenSource cts = new();
        Task loop = dispatcher.RunAsync(cts.Token);

        using Activity activity = new("req");
        activity.Start();

        await pipeline.LogAsync(
            "Catalog.Api",
            "request",
            LogLevel.Information,
            "hello",
            properties: new Dictionary<string, object?>
            {
                ["password"] = "p@ss",
                ["x"] = 1
            },
            cancellationToken: TestContext.Current.CancellationToken);

        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(5));

        activity.Stop();
        cts.Cancel();
        await loop;

        LogEntry entry = sink.Entries.Single();
        entry.Properties.Should().NotBeNull();
        entry.Properties!["password"].Should().Be("***");
        entry.Properties.Should().ContainKey("traceId");
        entry.Properties.Should().ContainKey("spanId");
    }

    [Fact]
    public async Task Middleware_ShouldLogFailure_WhenRequestThrows()
    {
        ServiceCollection services = new();
        CollectSink sink = new();
        services.AddCommonSharedKernelLogging(builder =>
        {
            builder.SetServiceName("Catalog.Api");
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddSink(sink);
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        LogDispatcher dispatcher = provider.GetRequiredService<LogDispatcher>();
        RequestLoggingMiddleware middleware = new(
            _ => throw new InvalidOperationException("boom"),
            provider.GetRequiredService<ILogger<RequestLoggingMiddleware>>(),
            provider.GetRequiredService<ILogContextAccessor>(),
            Options.Create(new LoggingMiddlewareOptions { IncludeRequestStartLog = false }),
            provider.GetRequiredService<IPayloadProtectionPipeline>(),
            provider.GetRequiredService<IPayloadStore>());

        using CancellationTokenSource cts = new();
        Task loop = dispatcher.RunAsync(cts.Token);

        DefaultHttpContext http = new();
        http.Request.Method = HttpMethods.Get;
        http.Request.Path = "/products";

        Func<Task> action = async () => await middleware.InvokeAsync(http);

        await action.Should().ThrowAsync<InvalidOperationException>();
        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(5));

        cts.Cancel();
        await loop;

        sink.Entries.Should().Contain(e => e.Category == "http.request.failed");
    }

    [Fact]
    public void JsonFormatter_ShouldEmitExceptionObject_WhenExceptionPresent()
    {
        JsonLogFormatter formatter = new();
        LogEntry entry = LogEntry.Create(
            LogLevel.Error,
            "Catalog",
            "Catalog.Api",
            "request",
            "failed",
            DateTimeOffset.UtcNow,
            exception: new InvalidOperationException("boom"));

        string payload = formatter.Format(entry);

        payload.Should().Contain("\"exception\"");
        payload.Should().Contain("InvalidOperationException");
    }

    [Fact]
    public void LogEntry_CreateWithoutLevel_ShouldDefaultToInformation()
    {
        LogEntry entry = LogEntry.Create(
            "Catalog",
            "Catalog.Api",
            "request",
            "created",
            DateTimeOffset.UtcNow);

        entry.Level.Should().Be(LogLevel.Information);
    }

    private sealed class NoopSink : ILogSink
    {
        public ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }

    private sealed class CollectSink : ILogSink
    {
        private readonly List<LogEntry> _entries = [];
        private readonly object _lock = new();

        public IReadOnlyList<LogEntry> Entries
        {
            get
            {
                lock (_lock)
                {
                    return _entries.ToArray();
                }
            }
        }

        public ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _entries.Add(entry);
            }

            return ValueTask.CompletedTask;
        }

        public async Task WaitForCountAsync(int count, TimeSpan timeout)
        {
            using CancellationTokenSource cts = new(timeout);
            while (!cts.IsCancellationRequested)
            {
                lock (_lock)
                {
                    if (_entries.Count >= count)
                    {
                        return;
                    }
                }

                await Task.Delay(20, cts.Token);
            }

            throw new TimeoutException($"Expected {count} log entries.");
        }
    }
}