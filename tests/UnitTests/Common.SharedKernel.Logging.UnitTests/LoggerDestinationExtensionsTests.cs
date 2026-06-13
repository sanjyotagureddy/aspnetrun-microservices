using NSubstitute;

namespace Common.SharedKernel.Logging.UnitTests;

public sealed class LoggerDestinationExtensionsTests
{
    [Fact]
    public async Task LogApplicationAsync_WithTraceLog_RoutesToTraceWithApplicationType()
    {
        ILogger logger = CreateLoggerSubstitute();
        TraceLog log = new() { Message = "trace" };
        CancellationToken token = TestContext.Current.CancellationToken;

        await logger.LogApplicationAsync(log, token);

        await logger.Received(1).LogTraceAsync(log, LogType.Application, token);
    }

    [Fact]
    public async Task LogEventAsync_WithApiLog_RoutesToApiWithEventType()
    {
        ILogger logger = CreateLoggerSubstitute();
        ApiLog log = new()
        {
            Message = "api",
            Method = "GET",
            Path = "/products",
            StatusCode = 200,
            DurationMs = 1
        };
        CancellationToken token = TestContext.Current.CancellationToken;

        await logger.LogEventAsync(log, token);

        await logger.Received(1).LogApiAsync(log, LogType.Event, token);
    }

    [Fact]
    public async Task LogAuditAsync_WithErrorLog_RoutesToErrorWithAuditType()
    {
        ILogger logger = CreateLoggerSubstitute();
        ErrorLog log = new()
        {
            Message = "error",
            Exception = new InvalidOperationException("boom")
        };
        CancellationToken token = TestContext.Current.CancellationToken;

        await logger.LogAuditAsync(log, token);

        await logger.Received(1).LogErrorAsync(log, LogType.Audit, token);
    }

    [Fact]
    public void LogSecurity_WithTraceLog_RoutesToTraceWithSecurityType()
    {
        ILogger logger = CreateLoggerSubstitute();
        TraceLog log = new() { Message = "security" };
        CancellationToken token = TestContext.Current.CancellationToken;

        logger.LogSecurity(log, token);

        logger.Received(1).LogTraceAsync(log, LogType.Security, token);
    }

    [Fact]
    public async Task LogApplicationAsync_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        Func<Task> act = async () => await LoggerDestinationExtensions.LogApplicationAsync(null!, new TraceLog { Message = "m" });

        ArgumentNullException exception = (await act.Should().ThrowAsync<ArgumentNullException>()).Which;
        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public async Task LogApplicationAsync_WhenLogIsNull_ThrowsArgumentNullException()
    {
        ILogger logger = CreateLoggerSubstitute();
        Func<Task> act = async () => await logger.LogApplicationAsync(null!);

        ArgumentNullException exception = (await act.Should().ThrowAsync<ArgumentNullException>()).Which;
        exception.ParamName.Should().Be("log");
    }

    [Fact]
    public async Task LogApplicationAsync_WithUnsupportedLogType_ThrowsArgumentException()
    {
        ILogger logger = CreateLoggerSubstitute();
        Func<Task> act = async () => await logger.LogApplicationAsync(new CustomLog { Message = "unsupported" });

        ArgumentException exception = (await act.Should().ThrowAsync<ArgumentException>()).Which;
        exception.ParamName.Should().Be("log");
        exception.Message.Should().Contain("Unsupported log model type");
    }

    private static ILogger CreateLoggerSubstitute()
    {
        ILogger logger = Substitute.For<ILogger>();

        logger.LogTraceAsync(Arg.Any<TraceLog>(), Arg.Any<LogType>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        logger.LogApiAsync(Arg.Any<ApiLog>(), Arg.Any<LogType>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        logger.LogErrorAsync(Arg.Any<ErrorLog>(), Arg.Any<LogType>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        return logger;
    }

    private sealed record CustomLog : BaseLog;
}
