using System.Data;
using System.Threading;

namespace BuildingBlocks.Outbox;

/// <summary>
/// Skeleton for transactional outbox: co-locate outbox table writes with domain changes
/// inside the same DB transaction and have a background publisher pick them up.
/// </summary>
public sealed class TransactionalOutbox
{
    // This is a conceptual example. Implementation is DB-specific and must be adapted.
    public record OutboxMessage(Guid Id, string Destination, string Payload, DateTime CreatedAt, bool Published);

    public Task SaveMessageAsync(IDbConnection dbConnection, IDbTransaction transaction, OutboxMessage message, CancellationToken cancellationToken = default)
    {
        // In the same transaction that writes the domain changes, insert the outbox row.
        // Example pseudo-code:
        // var sql = "INSERT INTO Outbox (Id, Destination, Payload, CreatedAt, Published) VALUES (...)";
        // Execute using dbConnection and transaction.
        return Task.CompletedTask;
    }

    public Task<IEnumerable<OutboxMessage>> GetUnpublishedAsync(IDbConnection dbConnection, CancellationToken cancellationToken = default)
    {
        // Query unpublished messages ordered by CreatedAt
        return Task.FromResult(Enumerable.Empty<OutboxMessage>());
    }

    public Task MarkPublishedAsync(IDbConnection dbConnection, IDbTransaction transaction, Guid messageId, CancellationToken cancellationToken = default)
    {
        // Mark outbox row as published within a transaction
        return Task.CompletedTask;
    }
}
