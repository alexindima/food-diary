using System.Data.Common;
using FoodDiary.Modules.Hydration.Application.Abstractions.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using Npgsql;

namespace FoodDiary.Modules.Hydration.Infrastructure.Persistence;

public sealed class EfHydrationOperationTransactionRunner(IModuleTransactionCoordinator coordinator) : IHydrationOperationTransactionRunner {
    public async Task<T> ExecuteSerializedAsync<T>(
        UserId userId,
        Guid operationId,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        return await coordinator.ExecuteAsync(async (transaction, token) => {
            await AcquireLockAsync(userId, operationId, transaction, token).ConfigureAwait(false);
            return await operation(token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static async Task AcquireLockAsync(
        UserId userId,
        Guid operationId,
        DbTransaction transaction,
        CancellationToken cancellationToken) {
        var postgresTransaction = (NpgsqlTransaction)transaction;
        var command = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(hashtextextended(@serialization_key, 0))",
            postgresTransaction.Connection,
            postgresTransaction);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue(
                "serialization_key",
                NpgsqlTypes.NpgsqlDbType.Text,
                $"hydration-operation:{userId.Value:N}:{operationId:N}");
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
