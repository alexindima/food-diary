using System.Data;
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
        return await ExecuteRelationalAsync(operation, isolationLevel: null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> ExecuteSerializableAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        SharedTransactionBoundary.EnsureCleanEntry(context, postCommitActionQueue);
        if (!context.Database.IsRelational()) {
            return await SharedTransactionBoundary.ExecuteAttemptAsync(context, postCommitActionQueue, async () => {
                T result = await operation(cancellationToken).ConfigureAwait(false);
                if (result is not FoodDiary.Results.Result { IsFailure: true } && unitOfWork.HasPendingChanges) {
                    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                return result;
            }, cancellationToken).ConfigureAwait(false);
        }
        return await ExecuteRelationalAsync((_, token) => operation(token), IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> ExecuteRelationalAsync<T>(
        Func<DbTransaction, CancellationToken, Task<T>> operation,
        IsolationLevel? isolationLevel,
        CancellationToken cancellationToken) {
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => SharedTransactionBoundary.ExecuteAttemptAsync(context, postCommitActionQueue, async () => {
            IDbContextTransaction transaction = isolationLevel.HasValue
                ? await context.Database.BeginTransactionAsync(isolationLevel.Value, cancellationToken).ConfigureAwait(false)
                : await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
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
