namespace Products.Api.Infrastructure.Outbox;

using Npgsql;

internal sealed class ProductOutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; init; }

    public string EventType { get; init; } = string.Empty;

    public string Topic { get; init; } = string.Empty;

    public string PayloadJson { get; init; } = string.Empty;

    public string MetadataJson { get; init; } = string.Empty;

    public int AttemptCount { get; init; }
}

internal sealed class ProductOutboxMetadata
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString("N");

    public string? CorrelationId { get; set; }

    public string? CausationId { get; set; }

    public string? TraceId { get; set; }

    public string? SpanId { get; set; }

    public string? TenantId { get; set; }

    public string? RoutingKey { get; set; }

    public string? OrderingKey { get; set; }

    public Common.SharedKernel.Messaging.MessageContractDescriptor Contract { get; set; } = Common.SharedKernel.Messaging.MessageContractDescriptor.Unspecified;

    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> TransportHints { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal interface IProductOutboxStore
{
    Task EnqueueAsync(ProductOutboxMessage message, CancellationToken cancellationToken);

    Task EnqueueAsync(ProductOutboxMessage message, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductOutboxMessage>> ClaimPendingAsync(int batchSize, TimeSpan claimDuration, CancellationToken cancellationToken);

    Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken);

    Task MarkFailedAsync(Guid id, int attemptCount, string error, CancellationToken cancellationToken);
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
