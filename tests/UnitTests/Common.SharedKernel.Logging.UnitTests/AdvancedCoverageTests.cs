using Microsoft.Extensions.Options;

namespace Common.SharedKernel.Logging.UnitTests;

public sealed class AdvancedCoverageTests
{
    [Fact]
    public async Task GenericLogger_ShouldSupportAllOverloads()
    {
        MemorySink sink = new();
        LogDispatcher dispatcher = CreateDispatcher(sink);
        LoggingPipeline pipeline = CreatePipeline(dispatcher, minimumLevel: LogLevel.Trace);
        Logger<object> logger = new(pipeline);

        using CancellationTokenSource cts = new();
        Task loop = dispatcher.RunAsync(cts.Token);
        CancellationToken token = TestContext.Current.CancellationToken;

        await logger.LogInformationAsync("info", category: null, properties: null, cancellationToken: token);
        await logger.LogErrorAsync(new InvalidOperationException("e1"), cancellationToken: token);
        await logger.LogCriticalAsync(new Exception("e2"), cancellationToken: token);
        await logger.LogErrorAsync("error-msg", "error.cat", new Exception("e3"), cancellationToken: token);
        await logger.LogCriticalAsync("critical-msg", "critical.cat", new Exception("e4"), cancellationToken: token);
        await logger.LogApiAsync("api-msg", cancellationToken: token);
        await logger.LogEventAsync("evt", cancellationToken: token);
        await logger.LogAuditAsync("aud", cancellationToken: token);
        await logger.LogSecurityAsync("sec", cancellationToken: token);

        await sink.WaitForCountAsync(9, TimeSpan.FromSeconds(5));

        cts.Cancel();
        await loop;

        sink.Entries.Should().Contain(e => e.Category == "exception");
        sink.Entries.Should().Contain(e => e.Category == "fatal");
        sink.Entries.Should().Contain(e => e.Category == "error.cat");
        sink.Entries.Should().Contain(e => e.Category == "critical.cat");
        sink.Entries.Should().Contain(e => e.Category == "api");
        sink.Entries.Should().Contain(e => e.Category == "event");
        sink.Entries.Should().Contain(e => e.Category == "audit");
        sink.Entries.Should().Contain(e => e.Category == "security");
        sink.Entries.Any(e =>
            e.Properties is not null
            && e.Properties.TryGetValue("logType", out object? value)
            && string.Equals(value?.ToString(), "api", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
        sink.Entries.Any(e =>
                e.Properties is not null
                && e.Properties.TryGetValue("logType", out object? value)
                && string.Equals(value?.ToString(), "event", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
        sink.Entries.Any(e =>
                e.Properties is not null
                && e.Properties.TryGetValue("logType", out object? value)
                && string.Equals(value?.ToString(), "audit", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
        sink.Entries.Any(e =>
                e.Properties is not null
                && e.Properties.TryGetValue("logType", out object? value)
                && string.Equals(value?.ToString(), "security", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    [Fact]
    public async Task LoggingPipeline_ShouldRespectMinimumLevelAndFilters()
    {
        MemorySink sink = new();
        LoggingOptions options = new()
        {
            ServiceName = "Catalog",
            MinimumLevel = LogLevel.Warning,
            BatchSize = 1,
            QueueCapacity = 32,
            CaptureActivityContext = false
        };

        LogDispatcher dispatcher = new([sink], Options.Create(options));
        LoggingPipeline pipeline = new(
            new LogContextAccessor(),
            Options.Create(options),
            [],
            [new CategoryPrefixFilter("allowed")],
            new DefaultLogRedactor(Options.Create(new LoggingPolicyOptions())),
            dispatcher,
            TimeProvider.System);

        using CancellationTokenSource cts = new();
        Task loop = dispatcher.RunAsync(cts.Token);

        await pipeline.LogAsync("Catalog.Api", "allowed.cat", LogLevel.Information, "below-min", cancellationToken: TestContext.Current.CancellationToken);
        await pipeline.LogAsync("Catalog.Api", "blocked.cat", LogLevel.Error, "blocked-by-filter", cancellationToken: TestContext.Current.CancellationToken);
        await pipeline.LogAsync("Catalog.Api", "allowed.cat", LogLevel.Error, "allowed", cancellationToken: TestContext.Current.CancellationToken);

        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(5));

        cts.Cancel();
        await loop;

        sink.Entries.Should().HaveCount(1);
        sink.Entries.Single().Message.Should().Be("allowed");
    }

    [Fact]
    public async Task LoggingHostedService_ShouldStartAndStop()
    {
        LogDispatcher dispatcher = new([], Options.Create(new LoggingOptions
        {
            ServiceName = "Catalog",
            BatchSize = 1,
            QueueCapacity = 8
        }));

        LoggingHostedService hostedService = new(dispatcher);

        await hostedService.StartAsync(TestContext.Current.CancellationToken);
        await hostedService.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ElasticsearchLogSink_ShouldAttemptSend_WhenEntriesExist()
    {
        ElasticsearchLogSink sink = new(new ElasticsearchSinkOptions
        {
            Endpoint = new Uri("http://127.0.0.1:9"),
            IndexName = "logs",
            Username = "user",
            Password = "pass"
        });

        LogEntry entry = LogEntry.Create(
            LogLevel.Information,
            "Catalog",
            "Catalog.Api",
            "request",
            "hello",
            DateTimeOffset.UtcNow);

        Func<Task> action = async () => await sink.WriteBatchAsync([entry], TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<Exception>();
    }

    private static LoggingPipeline CreatePipeline(LogDispatcher dispatcher, LogLevel minimumLevel)
    {
        LoggingOptions options = new()
        {
            ServiceName = "Catalog",
            MinimumLevel = minimumLevel,
            BatchSize = 1,
            QueueCapacity = 32
        };

        return new LoggingPipeline(
            new LogContextAccessor(),
            Options.Create(options),
            [],
            [],
            new DefaultLogRedactor(Options.Create(new LoggingPolicyOptions())),
            dispatcher,
            TimeProvider.System);
    }

    private static LogDispatcher CreateDispatcher(ILogSink sink)
        => new([sink], Options.Create(new LoggingOptions
        {
            ServiceName = "Catalog",
            MinimumLevel = LogLevel.Trace,
            BatchSize = 1,
            QueueCapacity = 32
        }));

    private sealed class MemorySink : ILogSink
    {
        private readonly List<LogEntry> _entries = [];
        private readonly object _gate = new();

        public IReadOnlyList<LogEntry> Entries
        {
            get
            {
                lock (_gate)
                {
                    return _entries.ToArray();
                }
            }
        }

        public ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
        {
            lock (_gate)
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
                lock (_gate)
                {
                    if (_entries.Count >= count)
                    {
                        return;
                    }
                }

                await Task.Delay(25, cts.Token);
            }

            throw new TimeoutException($"Expected at least {count} entries.");
        }
    }
}