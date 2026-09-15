using System.Data.Common;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using Npgsql;

namespace FoodDiary.Modules.Images.Infrastructure.Persistence;

public sealed class EfImageConfirmationTransactionRunner(IModuleTransactionCoordinator coordinator) : IImageConfirmationTransactionRunner {
    public async Task<T> ExecuteSerializedAsync<T>(
        ImageAssetId assetId,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        return await coordinator.ExecuteAsync(async (transaction, token) => {
            await AcquireLockAsync(assetId, transaction, token).ConfigureAwait(false);
            return await operation(token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static async Task AcquireLockAsync(
        ImageAssetId assetId,
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
                $"image-confirmation:{assetId.Value:N}");
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
