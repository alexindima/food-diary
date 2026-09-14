using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Infrastructure.Persistence.Locking;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Shared;

internal sealed class EfModuleSessionCoordinator(FoodDiaryDbContext context, IUnitOfWork unitOfWork) : IModuleSessionCoordinator {
    public async Task<T> ExecuteSerializedAsync<T>(
        string serializationKey,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(serializationKey);
        ArgumentNullException.ThrowIfNull(operation);
        SharedTransactionBoundary.EnsureCleanEntry(context);

        string connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("The session coordinator requires a relational connection string.");
        PostgresAdvisoryLockLease advisoryLock = await PostgresAdvisoryLockLease
            .AcquireAsync(connectionString, serializationKey, cancellationToken)
            .ConfigureAwait(false);
        await using (advisoryLock.ConfigureAwait(false)) {
            // SaveChanges owns its atomic transaction and retries. Never replay provider calls.
            return await SharedTransactionBoundary.ExecuteAttemptAsync(context, postCommitActionQueue: null, async () => {
                T result = await operation(cancellationToken).ConfigureAwait(false);
                if (result is not FoodDiary.Results.Result { IsFailure: true }) {
                    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                return result;
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
