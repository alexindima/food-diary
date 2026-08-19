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

        await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        bool lockAcquired = false;
        try {
            await ChangeSessionLockAsync(
                "SELECT pg_advisory_lock(hashtextextended(@serialization_key, 0))",
                serializationKey,
                cancellationToken).ConfigureAwait(false);
            lockAcquired = true;

            TResult result = await operation(cancellationToken).ConfigureAwait(false);

            IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            return result;
        } finally {
            try {
                if (lockAcquired) {
                    await ChangeSessionLockAsync(
                        "SELECT pg_advisory_unlock(hashtextextended(@serialization_key, 0))",
                        serializationKey,
                        CancellationToken.None).ConfigureAwait(false);
                }
            } finally {
                await context.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task ChangeSessionLockAsync(
        string commandText,
        string serializationKey,
        CancellationToken cancellationToken) {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        var command = new NpgsqlCommand(commandText, connection);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("serialization_key", NpgsqlTypes.NpgsqlDbType.Text, serializationKey);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
