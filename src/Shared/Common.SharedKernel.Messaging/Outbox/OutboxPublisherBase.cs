using Microsoft.Extensions.Hosting;
using Common.SharedKernel.Logging;

namespace Common.SharedKernel.Messaging.Outbox;

public abstract class OutboxPublisherBase<TOutboxMessage>(
    IOutboxStore<TOutboxMessage> outboxStore,
    ILogger logger,
    int batchSize = 50,
    TimeSpan? claimDuration = null,
    TimeSpan? idleDelay = null,
    bool backlogHeartbeatEnabled = false,
    TimeSpan? backlogHeartbeatInterval = null) : BackgroundService
    where TOutboxMessage : OutboxMessage
{
    private const int RetryEscalationAttemptThreshold = 3;
    private readonly TimeSpan _claimDuration = claimDuration ?? TimeSpan.FromSeconds(30);
    private readonly TimeSpan _idleDelay = idleDelay ?? TimeSpan.FromSeconds(3);
    private readonly bool _backlogHeartbeatEnabled = backlogHeartbeatEnabled;
    private readonly TimeSpan _backlogHeartbeatInterval = NormalizeBacklogHeartbeatInterval(backlogHeartbeatInterval);
    private readonly IOutboxBacklogReader? _backlogReader = outboxStore as IOutboxBacklogReader;

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
                    ["idleDelayMs"] = _idleDelay.TotalMilliseconds,
                    ["backlogHeartbeatEnabled"] = _backlogHeartbeatEnabled,
                    ["backlogHeartbeatIntervalMs"] = _backlogHeartbeatEnabled
                        ? _backlogHeartbeatInterval.TotalMilliseconds
                        : null
                }
            },
            stoppingToken);

        try
        {
            DateTime nextBacklogHeartbeatUtc = DateTime.UtcNow + _backlogHeartbeatInterval;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (ShouldEmitBacklogHeartbeat(nextBacklogHeartbeatUtc))
                {
                    await EmitBacklogHeartbeatAsync(stoppingToken);
                    nextBacklogHeartbeatUtc = DateTime.UtcNow + _backlogHeartbeatInterval;
                }

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
                        int nextAttempt = message.AttemptCount + 1;
                        int nextRetryDelaySeconds = ComputeNextRetryDelaySeconds(nextAttempt);

                        await outboxStore.MarkFailedAsync(message.Id, nextAttempt, ex.Message, stoppingToken);
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
                                    ["attempt"] = nextAttempt,
                                    ["nextRetryDelaySeconds"] = nextRetryDelaySeconds,
                                    ["retryEscalationThreshold"] = RetryEscalationAttemptThreshold
                                }
                            },
                            stoppingToken);

                        if (nextAttempt == RetryEscalationAttemptThreshold)
                        {
                            await logger.LogEventAsync(
                                new TraceLog
                                {
                                    Message = "Outbox message reached retry escalation threshold.",
                                    Category = "messaging.outbox.retry",
                                    Context = new Dictionary<string, object?>
                                    {
                                        ["outboxId"] = message.Id,
                                        ["eventType"] = message.EventType,
                                        ["topic"] = message.Topic,
                                        ["attempt"] = nextAttempt,
                                        ["nextRetryDelaySeconds"] = nextRetryDelaySeconds,
                                        ["threshold"] = RetryEscalationAttemptThreshold
                                    }
                                },
                                stoppingToken);
                        }
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

    private static int ComputeNextRetryDelaySeconds(int attemptCount)
        => Math.Min(300, Math.Max(5, (int)Math.Pow(2, attemptCount)));

    private static TimeSpan NormalizeBacklogHeartbeatInterval(TimeSpan? configuredInterval)
    {
        TimeSpan interval = configuredInterval ?? TimeSpan.FromMinutes(5);
        return interval < TimeSpan.FromSeconds(30)
            ? TimeSpan.FromSeconds(30)
            : interval;
    }

    private bool ShouldEmitBacklogHeartbeat(DateTime nextBacklogHeartbeatUtc)
        => _backlogHeartbeatEnabled
           && _backlogReader is not null
           && DateTime.UtcNow >= nextBacklogHeartbeatUtc;

    private async Task EmitBacklogHeartbeatAsync(CancellationToken cancellationToken)
    {
        if (_backlogReader is null)
        {
            return;
        }

        OutboxBacklogSnapshot snapshot = await _backlogReader.GetBacklogSnapshotAsync(cancellationToken);
        double? oldestPendingAgeMs = snapshot.OldestPendingOccurredOnUtc is { } oldestPendingOccurredOnUtc
            ? Math.Max(0, (DateTime.UtcNow - oldestPendingOccurredOnUtc).TotalMilliseconds)
            : null;

        await logger.LogEventAsync(
            new TraceLog
            {
                Message = "Outbox backlog heartbeat.",
                Category = "messaging.outbox.backlog",
                Context = new Dictionary<string, object?>
                {
                    ["pendingReady"] = snapshot.PendingReadyCount,
                    ["staleProcessing"] = snapshot.StaleProcessingCount,
                    ["oldestPendingAgeMs"] = oldestPendingAgeMs,
                    ["heartbeatIntervalMs"] = _backlogHeartbeatInterval.TotalMilliseconds
                }
            },
            cancellationToken);
    }

    protected abstract Task PublishAsync(TOutboxMessage outboxMessage, CancellationToken cancellationToken);
}
