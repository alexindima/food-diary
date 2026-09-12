using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FoodDiary.Infrastructure.Persistence.Meals;

public sealed class EfMealRecognitionTransactionRunner(
    FoodDiaryDbContext context,
    IUnitOfWork unitOfWork,
    IPostCommitActionQueue? postCommitActionQueue = null) : IMealRecognitionTransactionRunner {
    public async Task<T> ExecuteSerializedAsync<T>(UserId userId, Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        if (userId.Value == Guid.Empty) {
            throw new ArgumentException("A meal recognition transaction requires an owner.", nameof(userId));
        }
        SharedTransactionBoundary.EnsureCleanEntry(context, postCommitActionQueue);
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => SharedTransactionBoundary.ExecuteAttemptAsync(context, postCommitActionQueue, async () => {
            IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                await AcquireLockAsync(userId, transaction, cancellationToken).ConfigureAwait(false);
                T result = await operation(cancellationToken).ConfigureAwait(false);
                if (result is not FoodDiary.Results.Result { IsFailure: true }) {
                    if (unitOfWork.HasPendingChanges) {
                        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    }
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
                return result;
            }
        }, cancellationToken)).ConfigureAwait(false);
    }

    public async Task<uint> FlushCreatedMealAsync(MealId mealId, UserId userId, CancellationToken cancellationToken = default) {
        if (context.Database.CurrentTransaction is null) {
            throw new InvalidOperationException("A meal receipt must be captured inside its creation transaction.");
        }
        EntityEntry<Meal>? entry = context.ChangeTracker.Entries<Meal>()
            .SingleOrDefault(candidate => candidate.Entity.Id == mealId && candidate.Entity.UserId == userId);
        if (entry is null || entry.State != EntityState.Added) {
            throw new InvalidOperationException("Only the new owned meal can be flushed for a creation receipt.");
        }
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry.Property<uint>("xmin").CurrentValue;
    }

    private async Task AcquireLockAsync(UserId userId, IDbContextTransaction transaction, CancellationToken cancellationToken) {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended(@serialization_key, 0))",
            connection, (NpgsqlTransaction)transaction.GetDbTransaction());
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("serialization_key", NpgsqlTypes.NpgsqlDbType.Text, $"meal-recognition:{userId.Value:N}");
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
