using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Common.SharedKernel.Logging.IntegrationTests;

public sealed class LoggingPipelineIntegrationTests
{
    [Fact]
    public async Task AddCommonSharedKernelLogging_ShouldDispatchEntryToConfiguredSink()
    {
        var sink = Substitute.For<ILogSink>();
        var observedEntry = new TaskCompletionSource<LogEntry>(TaskCreationOptions.RunContinuationsAsynchronously);

        sink.WriteAsync(Arg.Any<LogEntry>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                observedEntry.TrySetResult(callInfo.ArgAt<LogEntry>(0));
                return ValueTask.CompletedTask;
            });

        var services = new ServiceCollection();
        services.AddCommonSharedKernelLogging(builder =>
        {
            builder.SetServiceName("CatalogService");
            builder.AddSink(sink);
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        LogDispatcher dispatcher = provider.GetRequiredService<LogDispatcher>();
        LoggingPipeline pipeline = provider.GetRequiredService<LoggingPipeline>();

        using CancellationTokenSource cancellationTokenSource = new();
        Task dispatcherTask = dispatcher.RunAsync(cancellationTokenSource.Token);

        await pipeline.LogAsync("ProductsApi", "Products", LogLevel.Information, "Product created", cancellationToken: cancellationTokenSource.Token);

        LogEntry entry = await observedEntry.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationTokenSource.Token);

        entry.ServiceName.Should().Be("CatalogService");
        entry.Category.Should().Be("Products");
        entry.Message.Should().Be("Product created");
        entry.Level.Should().Be(LogLevel.Information);

        cancellationTokenSource.Cancel();
        await dispatcherTask;
    }
}
