using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Common.SharedKernel.Logging.UnitTests;

public sealed class LoggingMiddlewareTests
{
    [Fact]
    public async Task RequestLoggingMiddleware_ShouldEmitStartAndCompletionLogs()
    {
        ServiceCollection services = new();
        CaptureSink sink = new();
        services.AddCommonSharedKernelLogging(builder =>
        {
            builder.SetServiceName("Catalog.Api");
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddSink(sink);
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        LogDispatcher dispatcher = provider.GetRequiredService<LogDispatcher>();
        ILogger<RequestLoggingMiddleware> logger =
            provider.GetRequiredService<ILogger<RequestLoggingMiddleware>>();
        ILogContextAccessor contextAccessor = provider.GetRequiredService<ILogContextAccessor>();
        IOptions<LoggingMiddlewareOptions> middlewareOptions = provider.GetRequiredService<IOptions<LoggingMiddlewareOptions>>();
        TimeProvider timeProvider = provider.GetRequiredService<TimeProvider>();

        RequestLoggingMiddleware middleware = new(
            async context =>
            {
                context.Response.StatusCode = StatusCodes.Status201Created;
                await Task.CompletedTask;
            },
            logger,
            contextAccessor,
            middlewareOptions,
            timeProvider);

        using CancellationTokenSource cts = new();
        Task loop = dispatcher.RunAsync(cts.Token);

        DefaultHttpContext http = new();
        http.Request.Method = HttpMethods.Post;
        http.Request.Path = "/products";
        http.Request.Headers[Constants.Headers.CorrelationId] = "corr-123";
        http.Request.Headers[Constants.Headers.TenantId] = "tenant-1";

        await middleware.InvokeAsync(http);
        await sink.WaitForCountAsync(2, TimeSpan.FromSeconds(5));

        cts.Cancel();
        await loop;

        sink.Entries.Should().Contain(e => e.Category == "http.request.start");
        sink.Entries.Should().Contain(e => e.Category == "http.request.complete");
        sink.Entries.Any(e =>
                e.Properties is not null
                && e.Properties.TryGetValue("statusCode", out object? value)
                && Equals(value, StatusCodes.Status201Created))
            .Should().BeTrue();
    }

    [Fact]
    public async Task RequestLoggingMiddleware_ShouldSkipExcludedRoutes()
    {
        CaptureSink sink = new();
        ServiceCollection services = new();
        services.AddCommonSharedKernelLogging(builder =>
        {
            builder.SetServiceName("Catalog.Api");
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddSink(sink);
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        LogDispatcher dispatcher = provider.GetRequiredService<LogDispatcher>();

        LoggingMiddlewareOptions options = new()
        {
            ExcludedRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/health" }
        };

        RequestLoggingMiddleware middleware = new(
            _ => Task.CompletedTask,
            provider.GetRequiredService<ILogger<RequestLoggingMiddleware>>(),
            provider.GetRequiredService<ILogContextAccessor>(),
            Options.Create(options),
            provider.GetRequiredService<TimeProvider>());

        using CancellationTokenSource cts = new();
        Task loop = dispatcher.RunAsync(cts.Token);

        DefaultHttpContext http = new();
        http.Request.Method = HttpMethods.Get;
        http.Request.Path = "/health/readiness";

        await middleware.InvokeAsync(http);
        await Task.Delay(150, TestContext.Current.CancellationToken);

        cts.Cancel();
        await loop;

        sink.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task LoggingStartupFilter_ShouldInsertMiddlewareAndInvokeNext()
    {
        ServiceCollection services = new();
        services.AddCommonSharedKernelLogging(builder =>
        {
            builder.SetServiceName("Catalog.Api");
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddSink(new CaptureSink());
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        ApplicationBuilder app = new(provider);

        bool nextCalled = false;
        LoggingStartupFilter startupFilter = new();
        Action<IApplicationBuilder> configure = startupFilter.Configure(_ => { nextCalled = true; });
        configure(app);

        RequestDelegate pipeline = app.Build();
        DefaultHttpContext http = new();
        http.Request.Path = "/products";

        await pipeline(http);

        nextCalled.Should().BeTrue();
    }

    private sealed class CaptureSink : ILogSink
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