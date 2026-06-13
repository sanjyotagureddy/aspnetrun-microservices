namespace Products.Api.Infrastructure.Outbox;

using Common.SharedKernel.Messaging.Outbox;
using Npgsql;

internal sealed class ProductOutboxMessage : OutboxMessage;

internal sealed class ProductOutboxMetadata : OutboxMetadata;

internal interface IProductOutboxStore : IOutboxStore<ProductOutboxMessage>, IOutboxBacklogReader
{
}

internal interface IProductDomainEventDispatcher
{
    Task DispatchAsync(
        IEnumerable<Common.SharedKernel.Abstractions.Events.IDomainEvent> domainEvents,
        Common.SharedKernel.Observability.Context.AppCallContextBase? appContext,
        NpgsqlConnection? connection,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken);
}
