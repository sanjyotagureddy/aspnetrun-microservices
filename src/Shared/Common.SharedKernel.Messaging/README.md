# Common.SharedKernel.Messaging

Broker-agnostic messaging abstractions and provider implementations for Shared Kernel applications.

## Architecture Decisions

- [ADR-0001: Broker-Agnostic Messaging Architecture](docs/ADR-0001-broker-agnostic-messaging-architecture.md)
- [Messaging Implementation Checklist](docs/Messaging-Implementation-Checklist.md)

Applications depend on:

- `IMessageBus`
- `IMessageProducer`
- `IMessageConsumer`
- `IMessageHandler<T>`
- `IMessageSerializer`
- `IMessagingProvider`

Provider implementations stay internal. Kafka is the first provider and is treated as an implementation detail.

## Usage

```csharp
services.AddMessaging(builder =>
{
    builder.UseKafka(options =>
    {
        options.BootstrapServers = "localhost:9092";
        options.ConsumerGroup = "order-service";
    });
});

await messageBus.PublishAsync(
    "orders.created",
    orderCreated,
    metadata =>
    {
        metadata.CorrelationId = correlationId;
        metadata.TenantId = tenantId;
        metadata.Headers["Source"] = "OrderService";
    },
    cancellationToken);
```

## Provider Strategy

Future providers should implement `IMessagingProvider` internally and register through a builder extension:

```csharp
services.AddMessaging(builder =>
{
    builder.UseRabbitMq();
});
```

Application publishing and consuming code should not change when providers change.

## Observability

The library emits:

- `ActivitySource`: `Common.SharedKernel.Messaging`
- `Meter`: `Common.SharedKernel.Messaging`
- Publish and consume duration histograms
- Retry, failure, dead-letter, and consumer lag metrics
- Structured logs through `Common.SharedKernel.Logging`

## Reliability

Current implementation includes:

- Retry with exponential backoff
- Manual or auto Kafka commits
- Structured dead-letter logging hooks
- Message metadata propagation
- Cancellation-aware producer and consumer paths
- Outbox publishing with lease-based claiming (`next_attempt_on_utc`)
- Reclaim of expired `processing` outbox rows after publisher crashes

## Outbox Semantics

Outbox stores use lease semantics to avoid duplicate work while allowing recovery from crashed publishers.

- `pending` rows are claimable when `next_attempt_on_utc` is null or in the past.
- `processing` rows are reclaimable when `next_attempt_on_utc <= now()` (expired lease).
- Claiming sets `status = processing` and refreshes lease via `next_attempt_on_utc`.
- Successful publish sets `status = published`, sets `processed_on_utc`, and clears `next_attempt_on_utc`.
- Failed publish sets `status = pending`, increments `attempt_count`, sets backoff in `next_attempt_on_utc`, and clears `processed_on_utc`.

## Domain Event To Integration Event Metadata

For aggregate-scoped streams, dispatchers set both `OrderingKey` and `RoutingKey` to aggregate id (for example `productId.ToString("N")`).

- Key values are serialized into outbox metadata JSON.
- Outbox publishers restore keys through `OutboxPublisherHelpers.CopyMetadata(...)`.
- Kafka producer enforces key presence for key-based partition strategies (`ByAggregateId`, `ByOrderingKey`, `ByRoutingKey`).

Next production hardening step: implement concrete dead-letter publishing and Testcontainers-backed integration tests for Kafka.
