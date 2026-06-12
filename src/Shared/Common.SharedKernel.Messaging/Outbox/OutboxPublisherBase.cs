using Microsoft.Extensions.Hosting;
using Common.SharedKernel.Logging;

namespace Common.SharedKernel.Messaging.Outbox;

public abstract class OutboxPublisherBase<TOutboxMessage>(
    IOutboxStore<TOutboxMessage> outboxStore,
    Common.SharedKernel.Logging.ILogger logger,
    int batchSize = 50,
    TimeSpan? claimDuration = null,
    TimeSpan? idleDelay = null) : BackgroundService
    where TOutboxMessage : OutboxMessage
{
    private readonly TimeSpan _claimDuration = claimDuration ?? TimeSpan.FromSeconds(30);
    private readonly TimeSpan _idleDelay = idleDelay ?? TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await logger.LogEventAsync(
            new TraceLog
            {
                Message = "Outbox publisher started.",
                Category = "messaging.outbox.lifecycle",
                Context = new Dictionary<string, object?>
                {
                    ["batchSize"] = batchSize,
                    ["claimDurationMs"] = _claimDuration.TotalMilliseconds,
                    ["idleDelayMs"] = _idleDelay.TotalMilliseconds
                }
            },
            stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                IReadOnlyList<TOutboxMessage> messages = await outboxStore.ClaimPendingAsync(batchSize, _claimDuration, stoppingToken);
                if (messages.Count == 0)
                {
                    await Task.Delay(_idleDelay, stoppingToken);
                    continue;
                }

                int publishedCount = 0;
                int failedCount = 0;

                foreach (TOutboxMessage message in messages)
                {
                    try
                    {
                        await PublishAsync(message, stoppingToken);
                        await outboxStore.MarkPublishedAsync(message.Id, stoppingToken);
                        publishedCount++;
                    }
                    catch (Exception ex)
                    {
                        failedCount++;

                        await outboxStore.MarkFailedAsync(message.Id, message.AttemptCount + 1, ex.Message, stoppingToken);
                        await logger.LogEventAsync(
                            new ErrorLog
                            {
                                Message = "Outbox publish failed",
                                Category = "messaging.outbox.publish",
                                Exception = ex,
                                ExceptionType = ex.GetType().FullName,
                                ExceptionMessage = ex.Message,
                                Context = new Dictionary<string, object?>
                                {
                                    ["outboxId"] = message.Id,
                                    ["eventType"] = message.EventType,
                                    ["topic"] = message.Topic,
                                    ["attempt"] = message.AttemptCount + 1
                                }
                            },
                            stoppingToken);
                    }
                }

                if (failedCount > 0 || messages.Count == batchSize)
                {
                    await logger.LogEventAsync(
                        new TraceLog
                        {
                            Message = "Outbox batch processed.",
                            Category = "messaging.outbox.batch",
                            Context = new Dictionary<string, object?>
                            {
                                ["claimed"] = messages.Count,
                                ["published"] = publishedCount,
                                ["failed"] = failedCount,
                                ["isBatchFull"] = messages.Count == batchSize
                            }
                        },
                        stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            await logger.LogEventAsync(
                new TraceLog
                {
                    Message = "Outbox publisher stopping due to cancellation.",
                    Category = "messaging.outbox.lifecycle"
                },
                CancellationToken.None);
        }
    }

    protected abstract Task PublishAsync(TOutboxMessage outboxMessage, CancellationToken cancellationToken);
}
