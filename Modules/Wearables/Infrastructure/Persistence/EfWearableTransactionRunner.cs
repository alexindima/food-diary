using FoodDiary.Infrastructure.Persistence.Shared;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Infrastructure.Persistence.Locking;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Wearables;

internal sealed class EfWearableTransactionRunner(FoodDiaryDbContext context) : IWearableTransactionRunner {
    public async Task<TResult> ExecuteSerializedAsync<TResult>(
        string serializationKey,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(serializationKey);
        ArgumentNullException.ThrowIfNull(operation);
        SharedTransactionBoundary.EnsureCleanEntry(context);

        string connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("The wearable transaction runner requires a relational connection string.");
        PostgresAdvisoryLockLease advisoryLock = await PostgresAdvisoryLockLease
            .AcquireAsync(connectionString, serializationKey, cancellationToken)
            .ConfigureAwait(false);
        await using (advisoryLock.ConfigureAwait(false)) {
            // SaveChanges owns its atomic transaction and retries. Never replay provider calls.
            return await SharedTransactionBoundary.ExecuteAttemptAsync(context, postCommitActionQueue: null, async () => {
                TResult result = await operation(cancellationToken).ConfigureAwait(false);
                if (result is not FoodDiary.Results.Result { IsFailure: true }) {
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                return result;
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
