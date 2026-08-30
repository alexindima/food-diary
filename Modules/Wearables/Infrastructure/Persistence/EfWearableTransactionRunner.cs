using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Infrastructure.Persistence.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Infrastructure.Persistence.Wearables;

internal sealed class EfWearableTransactionRunner(FoodDiaryDbContext context) : IWearableTransactionRunner {
    public async Task<TResult> ExecuteSerializedAsync<TResult>(
        string serializationKey,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(serializationKey);
        ArgumentNullException.ThrowIfNull(operation);

        string connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("The wearable transaction runner requires a relational connection string.");
        PostgresAdvisoryLockLease advisoryLock = await PostgresAdvisoryLockLease
            .AcquireAsync(connectionString, serializationKey, cancellationToken)
            .ConfigureAwait(false);
        await using (advisoryLock.ConfigureAwait(false)) {
            TResult result = await operation(cancellationToken).ConfigureAwait(false);

            IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
    }
}
