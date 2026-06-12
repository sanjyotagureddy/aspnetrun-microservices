using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Common.SharedKernel.Messaging.Outbox;

public abstract class OutboxPublisherBase<TOutboxMessage>(
    IOutboxStore<TOutboxMessage> outboxStore,
    ILogger logger,
    int batchSize = 50,
    TimeSpan? claimDuration = null,
    TimeSpan? idleDelay = null) : BackgroundService
    where TOutboxMessage : OutboxMessage
{
    private readonly TimeSpan _claimDuration = claimDuration ?? TimeSpan.FromSeconds(30);
    private readonly TimeSpan _idleDelay = idleDelay ?? TimeSpan.FromSeconds(3);

    protected virtual string FailureLogTemplate => "Failed to publish outbox message {OutboxId} for event type {EventType}.";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IReadOnlyList<TOutboxMessage> messages = await outboxStore.ClaimPendingAsync(batchSize, _claimDuration, stoppingToken);
            if (messages.Count == 0)
            {
                await Task.Delay(_idleDelay, stoppingToken);
                continue;
            }

            foreach (TOutboxMessage message in messages)
            {
                try
                {
                    await PublishAsync(message, stoppingToken);
                    await outboxStore.MarkPublishedAsync(message.Id, stoppingToken);
                }
                catch (Exception ex)
                {
                    await outboxStore.MarkFailedAsync(message.Id, message.AttemptCount + 1, ex.Message, stoppingToken);
                    logger.LogWarning(ex, FailureLogTemplate, message.Id, message.EventType);
                }
            }
        }
    }

    protected abstract Task PublishAsync(TOutboxMessage outboxMessage, CancellationToken cancellationToken);
}
