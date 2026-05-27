namespace Common.SharedKernel.Logging;

internal sealed class LoggingHostedService(LogDispatcher dispatcher) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => dispatcher.RunAsync(stoppingToken);
}
