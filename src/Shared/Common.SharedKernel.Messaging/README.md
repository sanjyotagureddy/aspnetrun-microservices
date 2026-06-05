# Common.SharedKernel.Messaging

Broker-agnostic messaging abstractions and provider implementations for Shared Kernel applications.

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

Next production hardening step: implement concrete dead-letter publishing and Testcontainers-backed integration tests for Kafka.
