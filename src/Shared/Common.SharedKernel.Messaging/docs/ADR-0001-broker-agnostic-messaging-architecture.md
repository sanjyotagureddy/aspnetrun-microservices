# ADR-0001: Broker-Agnostic Messaging Architecture

## Status
Proposed

## Tracking

- Implementation checklist: [Messaging-Implementation-Checklist.md](Messaging-Implementation-Checklist.md)

## Date
2026-06-06

## Context
The current `Common.SharedKernel.Messaging` library has strong Kafka coupling despite having provider abstractions.

Observed gaps:

- No first-class contract versioning model.
- Partitioning is exposed as a transport primitive (`Partition`) in shared metadata.
- Topic setup/provisioning is not implemented beyond a placeholder provider method.
- Core options and registration embed Kafka-specific concepts.
- Provider selection relies on enum switching in core registration.

This limits long-term extensibility to RabbitMQ, Azure Service Bus, and future transports while preserving clean boundaries and application-level stability.

## Decision
Refactor the messaging stack into a transport-neutral core plus provider packages, and introduce explicit contract, destination, and provisioning abstractions.

### 1. Package boundaries
Split responsibilities into packages/projects:

- `Common.SharedKernel.Messaging.Abstractions`
  - Contracts only (interfaces, records, enums for neutral messaging model).
- `Common.SharedKernel.Messaging.Core`
  - Default bus, serializer pipeline, metadata enrichment, provider-agnostic orchestration.
- `Common.SharedKernel.Messaging.Kafka`
  - Kafka provider, Kafka options, AdminClient provisioning, transport mapping.
- Future optional packages:
  - `Common.SharedKernel.Messaging.RabbitMq`
  - `Common.SharedKernel.Messaging.AzureServiceBus`

### 2. Neutral contract/version model
Introduce an explicit descriptor attached to every message envelope.

```csharp
namespace Common.SharedKernel.Messaging;

public sealed record MessageContractDescriptor(
    string MessageType,
    string Version,
    string ContentType,
    string? SchemaRef = null,
    string? CompatibilityMode = null);
```

Envelope shape:

```csharp
public interface IMessageEnvelope<out T>
{
    string MessageId { get; }
    string? CorrelationId { get; }
    string? CausationId { get; }
    string? TenantId { get; }
    string Destination { get; }
    DateTimeOffset TimestampUtc { get; }
    MessageContractDescriptor Contract { get; }
    IReadOnlyDictionary<string, string> Headers { get; }
    T Payload { get; }
}
```

Versioning policy:

- Non-breaking changes: same destination, higher compatible version in contract metadata.
- Breaking changes: new destination version (`*.v2`) plus migration window.

### 3. Destination and provisioning model
Replace topic-only provisioning with destination definitions.

```csharp
public enum DestinationKind
{
    Topic,
    Queue,
    Stream
}

public sealed record DestinationDefinition(
    string Name,
    DestinationKind Kind,
    int? PartitionCount = null,
    short? ReplicationFactor = null,
    TimeSpan? Retention = null,
    bool? EnableCompaction = null,
    IReadOnlyDictionary<string, string>? Attributes = null);

public enum ProvisioningMode
{
    ValidateOnly,
    CreateIfMissing,
    ReconcileNonBreaking
}

public interface IDestinationProvisioner
{
    Task EnsureAsync(
        IReadOnlyCollection<DestinationDefinition> definitions,
        ProvisioningMode mode,
        CancellationToken cancellationToken = default);
}
```

### 4. Neutral routing and ordering intent
Move transport-specific partition control out of shared metadata.

```csharp
public sealed class MessageMetadata
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
    public string? CorrelationId { get; set; }
    public string? CausationId { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? TenantId { get; set; }

    // Broker-agnostic semantics.
    public string? RoutingKey { get; set; }
    public string? OrderingKey { get; set; }

    // Optional provider extensions bag.
    public IDictionary<string, string> TransportHints { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, string> Headers { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
```

Provider-specific overrides (example):

- Kafka package may support explicit partition via provider publish options.
- This remains outside `Abstractions` and `Core` contracts.

### 5. Provider registration and capability model
Replace enum-based provider switching with explicit provider package registration.

```csharp
public sealed record MessagingProviderCapabilities(
    bool SupportsPartitioning,
    bool SupportsOrderingByKey,
    bool SupportsNativeDeadLetter,
    bool SupportsTransactions,
    bool SupportsDelayedDelivery);

public interface IMessagingProvider
{
    string Name { get; }
    MessagingProviderCapabilities Capabilities { get; }

    IMessageProducer CreateProducer();
    IMessageConsumer CreateConsumer();
    IDestinationProvisioner CreateProvisioner();

    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
```

Core registration:

```csharp
services.AddMessagingCore(options =>
{
    options.TopicPrefix = "dev";
    options.ProvisioningMode = ProvisioningMode.ValidateOnly;
});
```

Kafka package registration:

```csharp
services.AddKafkaMessaging(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.ConsumerGroup = "order-service";
});
```

## Migration Strategy
Use a staged rollout to avoid breaking all services at once.

### Phase 1 (backward compatible)

- Introduce `MessageContractDescriptor` and enrich envelope/headers.
- Introduce `DestinationDefinition` and `IDestinationProvisioner`.
- Keep existing `PublishAsync(topic, ...)` signatures with adapter logic.

### Phase 2 (provider decoupling)

- Move Kafka options and registration into Kafka package.
- Remove core enum provider switch.
- Replace `EnsureTopicAsync` usage with destination provisioner startup workflow.

### Phase 3 (strict governance)

- Add compatibility checks at consumer boundary.
- Add startup fail-fast for destination mismatch in non-development environments.
- Add provider contract test suite to validate required semantics across transports.

## Consequences

### Positive

- True broker agnosticism at application layer.
- Cleaner architecture boundaries and package ownership.
- Stronger evolution model for contract versioning.
- Better production readiness via provisioning and startup validation.

### Trade-offs

- Increased abstraction surface area.
- Requires migration path and compatibility shims.
- Potential provider feature gaps must be explicit through capabilities.

## Alternatives Considered

1. Keep single package and add more provider switches.
- Rejected: compounds coupling and brittle branching.

2. Make Kafka the permanent standard and document constraints.
- Rejected: conflicts with long-term extensibility and enterprise platform goals.

## Implementation Guardrails

- Shared contracts must remain transport-neutral.
- Provider implementation classes remain internal.
- No broker SDK references from `Abstractions` and `Core`.
- All provider-specific metadata keys must be namespaced (for example `kafka.partition`).
- Integration tests must include provisioning and publish/consume contract validation.

## Open Questions

1. Contract schema strategy: JSON compatibility only first, or schema registry from day one?
2. Production provisioning mode: `ValidateOnly` everywhere, or `CreateIfMissing` for selected environments?
3. Destination naming policy: global convention ownership by platform team vs service team autonomy?
4. Ordering policy enforcement: where to require `OrderingKey` for aggregate-scoped events?
