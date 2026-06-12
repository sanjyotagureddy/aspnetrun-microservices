using Microsoft.Extensions.Options;

namespace Common.SharedKernel.Logging.UnitTests;

public sealed class LoggerFactoryAndDispatcherTests
{
    [Fact]
    public async Task LoggingFactory_LoggerOverloads_ShouldDispatchEntries()
    {
        TestSink sink = new();
        LogDispatcher dispatcher = new([sink], Options.Create(new LoggingOptions
        {
            ServiceName = "Catalog",
            MinimumLevel = LogLevel.Trace,
            BatchSize = 1,
            QueueCapacity = 32,
            EnabledLogTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "*" }
        }));

        LoggingPipeline pipeline = new(
            new LogContextAccessor(),
            Options.Create(new LoggingOptions
            {
                ServiceName = "Catalog",
                MinimumLevel = LogLevel.Trace,
                BatchSize = 1,
                QueueCapacity = 32,
                EnabledLogTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "*" }
            }),
            [],
            [],
            new DefaultLogRedactor(Options.Create(new LoggingPolicyOptions())),
            dispatcher,
            TimeProvider.System);

        LoggingFactory factory = new(pipeline);
        ILogger logger = factory.CreateLogger("Catalog.Api");

        using CancellationTokenSource cts = new();
        Task loop = dispatcher.RunAsync(cts.Token);
        CancellationToken testToken = TestContext.Current.CancellationToken;

        await logger.LogTraceAsync(new TraceLog { Message = "trace" }, cancellationToken: testToken);
        await logger.LogApiAsync(new ApiLog { Message = "api", Method = "GET", Path = "/products", StatusCode = 200, DurationMs = 10 }, cancellationToken: testToken);
        await logger.LogErrorAsync(new ErrorLog { Message = "err", Exception = new InvalidOperationException("boom") }, cancellationToken: testToken);

        await sink.WaitForCountAsync(3, TimeSpan.FromSeconds(5));

        cts.Cancel();
        await loop;

        sink.Entries.Should().Contain(e => e.Message == "trace");
        sink.Entries.Should().Contain(e => e.Message == "api");
        sink.Entries.Should().Contain(e => e.Message == "err");
    }

    [Fact]
    public async Task LogDispatcher_ShouldIncrementFailureCountAndInvokeCallback_WhenSinkThrows()
    {
        Exception? observedException = null;
        string? observedSink = null;
        LoggingOptions options = new()
        {
            ServiceName = "Catalog",
            MinimumLevel = LogLevel.Trace,
            BatchSize = 1,
            QueueCapacity = 16,
            EnabledLogTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "*" },
            SinkFailureCallback = (ex, sinkName) =>
            {
                observedException = ex;
                observedSink = sinkName;
            }
        };

        LogDispatcher dispatcher = new([new ThrowingSink()], Options.Create(options));

        using CancellationTokenSource cts = new();
        Task loop = dispatcher.RunAsync(cts.Token);

        await dispatcher.EnqueueAsync(
            LogEntry.Create(LogLevel.Information, "Catalog", "Catalog.Api", "request", "msg", DateTimeOffset.UtcNow),
            CancellationToken.None);

        await Task.Delay(150, TestContext.Current.CancellationToken);
        cts.Cancel();
        await loop;

        dispatcher.SinkFailureCount.Should().BeGreaterThan(0);
        observedException.Should().NotBeNull();
        observedSink.Should().Be(nameof(ThrowingSink));
    }

    private sealed class ThrowingSink : ILogSink
    {
        public ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
            => ValueTask.FromException(new InvalidOperationException("sink failed"));
    }

    private sealed class TestSink : ILogSink
    {
        private readonly object _sync = new();
        private readonly List<LogEntry> _entries = [];

        public IReadOnlyList<LogEntry> Entries
        {
            get
            {
                lock (_sync)
                {
                    return _entries.ToArray();
                }
            }
        }

        public ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
        {
            lock (_sync)
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
                lock (_sync)
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