using Moq;
using Common.SharedKernel.Abstractions.Events;
using Common.SharedKernel.Results;
using Inventory.Api.Domain;
using Inventory.Api.Domain.Events;
using Inventory.Api.Features.Inventory.Initialize;
using Inventory.Api.Infrastructure;
using Inventory.Api.Infrastructure.Outbox;
using Inventory.Api.Infrastructure.Persistence;
using InventoryInitializedIntegrationEvent = global::Inventory.Api.Features.Inventory.Events.InventoryInitializedIntegrationEvent;
using Npgsql;

namespace Inventory.Api.Tests.Features.Inventory.Initialize;

public sealed class InitializeInventoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldInitializeStoreAndReturnSuccess()
    {
        FakeInventoryStore store = new();
        FakeInventoryDomainEventDispatcher dispatcher = new();
        FakeInventoryTransactionExecutor transactionExecutor = new();
        Mock<Common.SharedKernel.Logging.ILogger<InitializeInventoryCommandHandler>> logger = new();
        InitializeInventoryCommandHandler handler = new(store, new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)), logger.Object, dispatcher, transactionExecutor);
        Guid productId = Guid.NewGuid();

        Result result = await handler.Handle(new InitializeInventoryCommand(productId, 6), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(store.LastInitialized);
        Assert.Equal(productId, store.LastInitialized!.ProductId);
        Assert.Equal(6, store.LastInitialized.StockQuantity);
        Assert.Single(dispatcher.Events);
        Assert.Equal(InventoryInitializedDomainEvent.EventTypeName, dispatcher.Events[0].EventType);
        logger.Verify(
            x => x.LogTraceAsync(
                It.Is<Common.SharedKernel.Logging.TraceLog>(log =>
                    log.Category == "inventory_initialized"
                    && log.Operation == "inventory.initialize"
                    && log.Context != null
                    && log.Context.ContainsKey("aggregateType")
                    && Equals(log.Context["aggregateType"], "inventory-item")
                    && log.Context.ContainsKey("productId")
                    && Equals(log.Context["productId"], productId)
                    && log.Context.ContainsKey("stockQuantity")
                    && Equals(log.Context["stockQuantity"], 6)
                    && log.Context.ContainsKey("eventType")
                    && Equals(log.Context["eventType"], InventoryInitializedDomainEvent.EventTypeName)
                    && log.Context.ContainsKey("topic")
                    && Equals(log.Context["topic"], InventoryInitializedIntegrationEvent.Topic)
                    && log.Context.ContainsKey("occurredOnUtc")),
                Common.SharedKernel.Logging.LogType.Application,
                It.IsAny<CancellationToken>()),
            () => Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldLogErrorAndRethrow_WhenTransactionFails()
    {
        FakeInventoryStore store = new();
        FakeInventoryDomainEventDispatcher dispatcher = new();
        InvalidOperationException expectedException = new("simulated transaction failure");
        ThrowingInventoryTransactionExecutor transactionExecutor = new(expectedException);
        Mock<Common.SharedKernel.Logging.ILogger<InitializeInventoryCommandHandler>> logger = new();
        InitializeInventoryCommandHandler handler = new(store, new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)), logger.Object, dispatcher, transactionExecutor);
        Guid productId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new InitializeInventoryCommand(productId, 9), CancellationToken.None));

        logger.Verify(
            x => x.LogErrorAsync(
                It.Is<Common.SharedKernel.Logging.ErrorLog>(log =>
                    log.Category == "inventory_initialize_transaction_failed"
                    && log.Exception == expectedException
                    && log.Context != null
                    && log.Context.ContainsKey("operation")
                    && Equals(log.Context["operation"], "inventory.initialize")
                    && log.Context.ContainsKey("productId")
                    && Equals(log.Context["productId"], productId)
                    && log.Context.ContainsKey("stockQuantity")
                    && Equals(log.Context["stockQuantity"], 9)
                    && log.Context.ContainsKey("eventType")
                    && Equals(log.Context["eventType"], InventoryInitializedDomainEvent.EventTypeName)
                    && log.Context.ContainsKey("topic")
                    && Equals(log.Context["topic"], InventoryInitializedIntegrationEvent.Topic)
                    && log.Context.ContainsKey("occurredOnUtc")),
                Common.SharedKernel.Logging.LogType.Application,
                It.IsAny<CancellationToken>()),
            () => Times.Once());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeInventoryStore : IInventoryStore
    {
        public InventoryItem? LastInitialized { get; private set; }

        public Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
            => Task.FromResult<InventoryItem?>(null);

        public Task<IReadOnlyDictionary<Guid, int>> GetStockByProductIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyDictionary<Guid, int>)new Dictionary<Guid, int>());

        public Task InitializeAsync(InventoryItem item, CancellationToken cancellationToken)
        {
            LastInitialized = item;
            return Task.CompletedTask;
        }

        public Task InitializeAsync(InventoryItem item, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
            => InitializeAsync(item, cancellationToken);
    }

    private sealed class FakeInventoryDomainEventDispatcher : IInventoryDomainEventDispatcher
    {
        public List<IDomainEvent> Events { get; } = [];

        public Task DispatchAsync(
            IEnumerable<IDomainEvent> domainEvents,
            Common.SharedKernel.Observability.Context.AppCallContextBase? appContext,
            NpgsqlConnection? connection,
            NpgsqlTransaction? transaction,
            CancellationToken cancellationToken)
        {
            Events.AddRange(domainEvents);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInventoryTransactionExecutor : IInventoryTransactionExecutor
    {
        public Task ExecuteAsync(Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task> operation, CancellationToken cancellationToken)
            => operation(null!, null!, cancellationToken);

        public Task<T> ExecuteAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
            => operation(null!, null!, cancellationToken);
    }

    private sealed class ThrowingInventoryTransactionExecutor(Exception exception) : IInventoryTransactionExecutor
    {
        public Task ExecuteAsync(Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task> operation, CancellationToken cancellationToken)
            => Task.FromException(exception);

        public Task<T> ExecuteAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
            => Task.FromException<T>(exception);
    }
}
