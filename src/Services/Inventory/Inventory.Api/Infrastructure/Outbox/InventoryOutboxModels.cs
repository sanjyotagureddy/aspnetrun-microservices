namespace Inventory.Api.Infrastructure.Outbox;

using Common.SharedKernel.Messaging.Outbox;
using Npgsql;

internal sealed class InventoryOutboxMessage : OutboxMessage;

internal sealed class InventoryOutboxMetadata : OutboxMetadata;

internal interface IInventoryOutboxStore : IOutboxStore<InventoryOutboxMessage>, IOutboxBacklogReader
{
}

internal interface IInventoryDomainEventDispatcher
{
    Task DispatchAsync(
        IEnumerable<Common.SharedKernel.Abstractions.Events.IDomainEvent> domainEvents,
        Common.SharedKernel.Observability.Context.AppCallContextBase? appContext,
        NpgsqlConnection? connection,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken);
}
