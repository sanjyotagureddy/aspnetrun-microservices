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

        await logger.LogTraceAsync(new TraceLog { Message = "info" }, cancellationToken: token);
        await logger.LogErrorAsync(new ErrorLog { Message = "error-msg", Category = "error.cat", Exception = new InvalidOperationException("e1") }, cancellationToken: token);
        await logger.LogApiAsync(
            new ApiLog { Message = "api-msg", Method = "GET", Path = "/products", StatusCode = 200, DurationMs = 5 },
            cancellationToken: token);
        await logger.LogEventAsync(new TraceLog { Message = "evt", Category = "event" }, token);
        await logger.LogAuditAsync(new TraceLog { Message = "aud", Category = "audit" }, token);
        await logger.LogSecurityAsync(new TraceLog { Message = "sec", Category = "security" }, token);

        await sink.WaitForCountAsync(6, TimeSpan.FromSeconds(5));

        cts.Cancel();
        await loop;

        sink.Entries.Should().Contain(e => e.Category == "error.cat");
        sink.Entries.Should().Contain(e => e.Category == "api");
        sink.Entries.Should().Contain(e => e.Category == "event");
        sink.Entries.Should().Contain(e => e.Category == "audit");
        sink.Entries.Should().Contain(e => e.Category == "security");
        sink.Entries.Any(e =>
            e.Properties is not null
            && e.Properties.TryGetValue("logType", out object? value)
            && string.Equals(value?.ToString(), "app", StringComparison.OrdinalIgnoreCase))
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
            CaptureActivityContext = false,
            EnabledLogTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "*" }
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
            QueueCapacity = 32,
            EnabledLogTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "*" }
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
            QueueCapacity = 32,
            EnabledLogTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "*" }
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