using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text;

namespace Common.SharedKernel.Logging.UnitTests;

public sealed class LoggingMiddlewareTests
{
    [Fact]
    public async Task RequestLoggingMiddleware_ShouldEmitCompletionLogOnly_OnSuccess()
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
        ILogger<RequestLoggingMiddlewareBase> logger =
            provider.GetRequiredService<ILogger<RequestLoggingMiddlewareBase>>();
        ILogContextAccessor contextAccessor = provider.GetRequiredService<ILogContextAccessor>();
        IOptions<LoggingMiddlewareOptions> middlewareOptions = provider.GetRequiredService<IOptions<LoggingMiddlewareOptions>>();

        RequestLoggingMiddleware middleware = new(
            async context =>
            {
                context.Response.StatusCode = StatusCodes.Status201Created;
                await Task.CompletedTask;
            },
            logger,
            contextAccessor,
            middlewareOptions,
            provider.GetRequiredService<IPayloadProtectionPipeline>(),
            provider.GetRequiredService<IPayloadStore>());

        using CancellationTokenSource cts = new();
        Task loop = dispatcher.RunAsync(cts.Token);

        DefaultHttpContext http = new();
        http.Request.Method = HttpMethods.Post;
        http.Request.Path = "/products";
        http.Request.Headers[Constants.Headers.CorrelationId] = "corr-123";
        http.Request.Headers[Constants.Headers.TenantId] = "tenant-1";

        await middleware.InvokeAsync(http);
        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(5));

        cts.Cancel();
        await loop;

        sink.Entries.Should().Contain(e => e.Category == "app_request");
        sink.Entries.Should().NotContain(e => e.Category == "http.request.start");
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
            provider.GetRequiredService<ILogger<RequestLoggingMiddlewareBase>>(),
            provider.GetRequiredService<ILogContextAccessor>(),
            Options.Create(options),
            provider.GetRequiredService<IPayloadProtectionPipeline>(),
            provider.GetRequiredService<IPayloadStore>());

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
        LoggingStartupFilter startupFilter = new(new RequestLoggingMiddlewareRegistration(typeof(RequestLoggingMiddleware)));
        Action<IApplicationBuilder> configure = startupFilter.Configure(_ => { nextCalled = true; });
        configure(app);

        RequestDelegate pipeline = app.Build();
        DefaultHttpContext http = new();
        http.Request.Path = "/products";

        await pipeline(http);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RequestLoggingMiddleware_ShouldCaptureOnlyAllowedHeaders()
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
        ILogger<RequestLoggingMiddlewareBase> logger =
            provider.GetRequiredService<ILogger<RequestLoggingMiddlewareBase>>();
        ILogContextAccessor contextAccessor = provider.GetRequiredService<ILogContextAccessor>();
        IOptions<LoggingMiddlewareOptions> middlewareOptions = provider.GetRequiredService<IOptions<LoggingMiddlewareOptions>>();

        RequestLoggingMiddleware middleware = new(
            context =>
            {
                context.Response.Headers[Constants.Headers.CorrelationId] = "corr-response-1";
                context.Response.Headers["x-not-allowed-response"] = "secret-response";
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            logger,
            contextAccessor,
            middlewareOptions,
            provider.GetRequiredService<IPayloadProtectionPipeline>(),
            provider.GetRequiredService<IPayloadStore>());

        using CancellationTokenSource cts = new();
        Task loop = dispatcher.RunAsync(cts.Token);

        DefaultHttpContext http = new();
        http.Request.Method = HttpMethods.Get;
        http.Request.Path = "/products";
        http.Request.Headers[Constants.Headers.CorrelationId] = "corr-request-1";
        http.Request.Headers["x-not-allowed-request"] = "secret-request";

        await middleware.InvokeAsync(http);
        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(5));

        cts.Cancel();
        await loop;

        LogEntry completionEntry = sink.Entries.Single(e => e.Category == "app_request");
        completionEntry.Properties.Should().NotBeNull();
        completionEntry.Properties!.Should().ContainKey("rq.x-correlationid");
        completionEntry.Properties.Should().ContainKey("rs.x-correlationid");
        completionEntry.Properties.Should().NotContainKey("rq.x-not-allowed-request");
        completionEntry.Properties.Should().NotContainKey("rs.x-not-allowed-response");
    }

    [Fact]
    public async Task RequestLoggingMiddleware_ShouldAddDistinctRequestAndResponsePayloadUrls_WhenPayloadStored()
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

        RequestLoggingMiddleware middleware = new(
            async context =>
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsync("{\"ok\":true}", TestContext.Current.CancellationToken);
            },
            provider.GetRequiredService<ILogger<RequestLoggingMiddlewareBase>>(),
            provider.GetRequiredService<ILogContextAccessor>(),
            provider.GetRequiredService<IOptions<LoggingMiddlewareOptions>>(),
            provider.GetRequiredService<IPayloadProtectionPipeline>(),
            new FixedPayloadStore("http://log-store/api/v1/logs/payload-123"));

        using CancellationTokenSource cts = new();
        Task loop = dispatcher.RunAsync(cts.Token);

        DefaultHttpContext http = new();
        http.Request.Method = HttpMethods.Post;
        http.Request.Path = "/products/1";
        http.Request.ContentType = "application/json";
        http.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"id\":1}"));

        await middleware.InvokeAsync(http);
        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(5));

        cts.Cancel();
        await loop;

        LogEntry completionEntry = sink.Entries.Single(e => e.Category == "app_request");
        completionEntry.Properties.Should().NotBeNull();
        completionEntry.Properties!.Should().ContainKey("request.url");
        completionEntry.Properties.Should().ContainKey("response.url");
        completionEntry.Properties["request.url"].Should().NotBe(completionEntry.Properties["response.url"]);
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

    private sealed class FixedPayloadStore(string payloadRef) : IPayloadStore
    {
        private readonly string _payloadRef = payloadRef;
        private int _counter;

        public Task<PayloadStoreWriteResult> StoreAsync(PayloadStoreWriteRequest request, CancellationToken cancellationToken = default)
        {
            int callNumber = Interlocked.Increment(ref _counter);
            return Task.FromResult(new PayloadStoreWriteResult(
                PayloadRef: $"{_payloadRef}-{callNumber}",
                PayloadHash: $"hash-123-{callNumber}",
                PayloadSizeBytes: 123,
                PayloadEncoding: "application/json",
                Compressed: false,
                Encrypted: false));
        }
    }

}
