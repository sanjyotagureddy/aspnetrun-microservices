using Npgsql;

namespace Inventory.Api.Infrastructure.Persistence;

internal interface IInventoryTransactionExecutor
{
    Task ExecuteAsync(Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task> operation, CancellationToken cancellationToken);

    Task<T> ExecuteAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}

internal sealed class InventoryTransactionExecutor(NpgsqlDataSource dataSource) : IInventoryTransactionExecutor
{
    public async Task ExecuteAsync(Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<T> ExecuteAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            T result = await operation(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
