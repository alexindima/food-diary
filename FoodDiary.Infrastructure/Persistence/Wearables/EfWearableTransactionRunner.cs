using FoodDiary.Application.Abstractions.Wearables.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FoodDiary.Infrastructure.Persistence.Wearables;

internal sealed class EfWearableTransactionRunner(FoodDiaryDbContext context) : IWearableTransactionRunner {
    public async Task<TResult> ExecuteSerializedAsync<TResult>(
        string serializationKey,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(serializationKey);
        ArgumentNullException.ThrowIfNull(operation);

        IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false)) {
            await AcquireTransactionLockAsync(serializationKey, transaction, cancellationToken).ConfigureAwait(false);
            TResult result = await operation(cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
    }

    private async Task AcquireTransactionLockAsync(
        string serializationKey,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken) {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        var command = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(hashtextextended(@serialization_key, 0))",
            connection,
            (NpgsqlTransaction)transaction.GetDbTransaction());
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("serialization_key", NpgsqlTypes.NpgsqlDbType.Text, serializationKey);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
