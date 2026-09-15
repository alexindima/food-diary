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

    public async Task<bool> ExecuteItemAsync(
        Func<DbTransaction, CancellationToken, Task<bool>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        SharedTransactionBoundary.EnsureCleanEntry(context);
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => SharedTransactionBoundary.ExecuteAttemptAsync(context, postCommitActionQueue: null, async () => {
            IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                bool processed = await operation(transaction.GetDbTransaction(), cancellationToken).ConfigureAwait(false);
                if (processed) {
                    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return processed;
            }
        }, cancellationToken)).ConfigureAwait(false);
    }

    public async Task<T> ExecuteAsync<T>(
        Func<DbTransaction, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        SharedTransactionBoundary.EnsureCleanEntry(context, postCommitActionQueue);
        return await ExecuteRelationalAsync(operation, isolationLevel: null, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteAsync(
        Func<DbTransaction, CancellationToken, Task> operation,
        Func<Exception, Exception> translateException,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(translateException);
        SharedTransactionBoundary.EnsureCleanEntry(context, postCommitActionQueue);
        await ExecuteRelationalAsync(async (transaction, token) => {
            await operation(transaction, token).ConfigureAwait(false);
            return true;
        }, isolationLevel: null, cancellationToken, alwaysSave: true, translateException).ConfigureAwait(false);
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
        CancellationToken cancellationToken,
        bool alwaysSave = false,
        Func<Exception, Exception>? translateException = null) {
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => SharedTransactionBoundary.ExecuteAttemptAsync(context, postCommitActionQueue, async () => {
            try {
                IDbContextTransaction transaction = isolationLevel.HasValue
                    ? await context.Database.BeginTransactionAsync(isolationLevel.Value, cancellationToken).ConfigureAwait(false)
                    : await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                await using (transaction.ConfigureAwait(false)) {
                    T result = await operation(transaction.GetDbTransaction(), cancellationToken).ConfigureAwait(false);
                    if (result is not FoodDiary.Results.Result { IsFailure: true } && (alwaysSave || unitOfWork.HasPendingChanges)) {
                        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    }

                    if (result is not FoodDiary.Results.Result { IsFailure: true }) {
                        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    }
                    return result;
                }
            } catch (Exception exception) when (translateException is not null) {
                Exception translated = translateException(exception);
                if (ReferenceEquals(exception, translated)) {
                    throw;
                }
                throw translated;
            }
        }, cancellationToken)).ConfigureAwait(false);
    }
}
