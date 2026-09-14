using System.Data.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Infrastructure.Persistence.Shared;

internal sealed class EfModuleTransactionCoordinator(
    FoodDiaryDbContext context,
    IUnitOfWork unitOfWork,
    IPostCommitActionQueue? postCommitActionQueue = null) : IModuleTransactionCoordinator {
    public DbTransaction? CurrentTransaction => context.Database.CurrentTransaction?.GetDbTransaction();

    public async Task<T> ExecuteAsync<T>(
        Func<DbTransaction, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        SharedTransactionBoundary.EnsureCleanEntry(context, postCommitActionQueue);
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => SharedTransactionBoundary.ExecuteAttemptAsync(context, postCommitActionQueue, async () => {
            IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                T result = await operation(transaction.GetDbTransaction(), cancellationToken).ConfigureAwait(false);
                if (result is not FoodDiary.Results.Result { IsFailure: true } && unitOfWork.HasPendingChanges) {
                    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                if (result is not FoodDiary.Results.Result { IsFailure: true }) {
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
                return result;
            }
        }, cancellationToken)).ConfigureAwait(false);
    }
}
