using FoodDiary.Modules.Meals.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;

namespace FoodDiary.Infrastructure.Persistence.Meals;

public sealed class EfMealRecognitionTransactionRunner(
    IModuleTransactionCoordinator coordinator,
    MealsDbContext meals,
    IUnitOfWork unitOfWork) : IMealRecognitionTransactionRunner {
    public async Task<T> ExecuteSerializedAsync<T>(UserId userId, Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        if (userId.Value == Guid.Empty) {
            throw new ArgumentException("A meal recognition transaction requires an owner.", nameof(userId));
        }
        return await coordinator.ExecuteAsync(async (transaction, token) => {
            await AcquireLockAsync(userId, transaction, token).ConfigureAwait(false);
            return await operation(token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<uint> FlushCreatedMealAsync(MealId mealId, UserId userId, CancellationToken cancellationToken = default) {
        if (coordinator.CurrentTransaction is null) {
            throw new InvalidOperationException("A meal receipt must be captured inside its creation transaction.");
        }
        EntityEntry<Meal>? entry = meals.ChangeTracker.Entries<Meal>()
            .SingleOrDefault(candidate => candidate.Entity.Id == mealId && candidate.Entity.UserId == userId);
        if (entry is null || entry.State != EntityState.Added) {
            throw new InvalidOperationException("Only the new owned meal can be flushed for a creation receipt.");
        }
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry.Property<uint>("xmin").CurrentValue;
    }

    private static async Task AcquireLockAsync(UserId userId, DbTransaction transaction, CancellationToken cancellationToken) {
        var postgresTransaction = (NpgsqlTransaction)transaction;
        var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended(@serialization_key, 0))",
            postgresTransaction.Connection, postgresTransaction);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("serialization_key", NpgsqlTypes.NpgsqlDbType.Text, $"meal-recognition:{userId.Value:N}");
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
