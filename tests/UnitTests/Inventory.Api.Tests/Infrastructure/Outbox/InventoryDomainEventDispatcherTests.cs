using System.Text.Json;
using Common.SharedKernel.Abstractions.Events;
using Common.SharedKernel.Messaging.Outbox;
using Inventory.Api.Domain.Events;
using Inventory.Api.Infrastructure.Outbox;

namespace Inventory.Api.Tests.Infrastructure.Outbox;

public sealed class InventoryDomainEventDispatcherTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task DispatchAsync_ShouldPersistAggregateRoutingAndOrderingKeysInOutboxMetadata()
    {
        FakeInventoryOutboxStore outboxStore = new();
        InventoryDomainEventDispatcher dispatcher = new(outboxStore);
        Guid productId = Guid.NewGuid();

        IDomainEvent[] domainEvents =
        [
            new InventoryInitializedDomainEvent(
                DateTime.UtcNow,
                productId,
                13,
                DateTime.UtcNow,
                DateTime.UtcNow)
        ];

        await dispatcher.DispatchAsync(domainEvents, appContext: null, connection: null, transaction: null, CancellationToken.None);

        outboxStore.Messages.Should().ContainSingle();
        InventoryOutboxMessage outboxMessage = outboxStore.Messages[0];
        InventoryOutboxMetadata metadata = JsonSerializer.Deserialize<InventoryOutboxMetadata>(outboxMessage.MetadataJson, JsonOptions)!;

        metadata.OrderingKey.Should().Be(productId.ToString("N"));
        metadata.RoutingKey.Should().Be(productId.ToString("N"));
    }

    private sealed class FakeInventoryOutboxStore : IInventoryOutboxStore
    {
        public List<InventoryOutboxMessage> Messages { get; } = [];

        public Task EnqueueAsync(InventoryOutboxMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task EnqueueAsync(InventoryOutboxMessage message, Npgsql.NpgsqlConnection connection, Npgsql.NpgsqlTransaction transaction, CancellationToken cancellationToken)
            => EnqueueAsync(message, cancellationToken);

        public Task<IReadOnlyList<InventoryOutboxMessage>> ClaimPendingAsync(int batchSize, TimeSpan claimDuration, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyList<InventoryOutboxMessage>)[]);

        public Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task MarkFailedAsync(Guid id, int attemptCount, string error, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<OutboxBacklogSnapshot> GetBacklogSnapshotAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
