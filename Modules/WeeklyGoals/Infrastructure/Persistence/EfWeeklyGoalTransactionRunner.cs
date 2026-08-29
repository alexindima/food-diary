using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.WeeklyGoals.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FoodDiary.Infrastructure.Persistence.WeeklyGoals;

public sealed class EfWeeklyGoalTransactionRunner(
    FoodDiaryDbContext context,
    IUnitOfWork unitOfWork) : IWeeklyGoalTransactionRunner {
    public async Task<T> ExecuteSerializedAsync<T>(
        UserId userId,
        DateTime weekStartUtc,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () => {
            IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                await AcquireLockAsync(userId, weekStartUtc, transaction, cancellationToken).ConfigureAwait(false);
                T result = await operation(cancellationToken).ConfigureAwait(false);
                if (unitOfWork.HasPendingChanges) {
                    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
        }).ConfigureAwait(false);
    }

    private async Task AcquireLockAsync(
        UserId userId,
        DateTime weekStartUtc,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken) {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        var command = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(hashtextextended(@serialization_key, 0))",
            connection,
            (NpgsqlTransaction)transaction.GetDbTransaction());
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue(
                "serialization_key",
                NpgsqlTypes.NpgsqlDbType.Text,
                $"weekly-goal:{userId.Value:N}:{weekStartUtc:O}");
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
