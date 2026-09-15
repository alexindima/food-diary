using System.Data.Common;
using FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using Npgsql;

namespace FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence;

public sealed class EfWeeklyGoalTransactionRunner(IModuleTransactionCoordinator coordinator) : IWeeklyGoalTransactionRunner {
    public async Task<T> ExecuteSerializedAsync<T>(
        UserId userId,
        DateTime weekStartUtc,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        return await coordinator.ExecuteAsync(async (transaction, token) => {
            await AcquireLockAsync(userId, weekStartUtc, transaction, token).ConfigureAwait(false);
            return await operation(token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static async Task AcquireLockAsync(
        UserId userId,
        DateTime weekStartUtc,
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
                $"weekly-goal:{userId.Value:N}:{weekStartUtc:O}");
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
