using FoodDiary.Modules.Lessons.Domain.Contracts.ValueObjects.Ids;
using System.Data.Common;
using FoodDiary.Modules.Lessons.Application.Abstractions.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using Npgsql;

namespace FoodDiary.Modules.Lessons.Infrastructure.Persistence;

public sealed class EfLessonProgressTransactionRunner(IModuleTransactionCoordinator coordinator) : ILessonProgressTransactionRunner {
    public async Task<T> ExecuteSerializedAsync<T>(
        UserId userId,
        NutritionLessonId lessonId,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        return await coordinator.ExecuteAsync(async (transaction, token) => {
            await AcquireLockAsync(userId, lessonId, transaction, token).ConfigureAwait(false);
            return await operation(token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static async Task AcquireLockAsync(
        UserId userId,
        NutritionLessonId lessonId,
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
                $"lesson-progress:{userId.Value:N}:{lessonId.Value:N}");
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
