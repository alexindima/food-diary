using FoodDiary.Persistence.PostgreSql;
using FoodDiary.Results;
using System.Data;
using System.Data.Common;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Persistence.Runtime.Persistence.Shared;

internal sealed class EfModuleTransactionCoordinator(
    SharedPersistenceDbContext context,
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
            IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await using (transaction) {
                return await context.Session.WithTransactionAsync(transaction.GetDbTransaction(), async () => {
                    bool processed = await operation(transaction.GetDbTransaction(), cancellationToken);
                    if (processed) {
                        await unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    await VerifiedTransactionCommit.CommitAsync(transaction, cancellationToken);
                    return processed;
                }, cancellationToken);
            }
        }, cancellationToken));
    }

    public async Task<T> ExecuteAsync<T>(
        Func<DbTransaction, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        SharedTransactionBoundary.EnsureCleanEntry(context, postCommitActionQueue);
        return await ExecuteRelationalAsync(operation, isolationLevel: null, cancellationToken);
    }

    public async Task ExecuteAsync(
        Func<DbTransaction, CancellationToken, Task> operation,
        Func<Exception, Exception> translateException,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(translateException);
        SharedTransactionBoundary.EnsureCleanEntry(context, postCommitActionQueue);
        await ExecuteRelationalAsync(async (transaction, token) => {
            await operation(transaction, token);
            return true;
        }, isolationLevel: null, cancellationToken, alwaysSave: true, translateException);
    }

    public async Task<T> ExecuteSerializableAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        SharedTransactionBoundary.EnsureCleanEntry(context, postCommitActionQueue);
        if (!context.Database.IsRelational()) {
            return await SharedTransactionBoundary.ExecuteAttemptAsync(context, postCommitActionQueue, async () => {
                T result = await operation(cancellationToken);
                if (result is not Result { IsFailure: true } && unitOfWork.HasPendingChanges) {
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
                return result;
            }, cancellationToken);
        }
        return await ExecuteRelationalAsync((_, token) => operation(token), IsolationLevel.Serializable, cancellationToken);
    }

    private async Task<T> ExecuteRelationalAsync<T>(
        Func<DbTransaction, CancellationToken, Task<T>> operation,
        IsolationLevel? isolationLevel,
        CancellationToken cancellationToken,
        bool alwaysSave = false,
        Func<Exception, Exception>? translateException = null) {
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => SharedTransactionBoundary.ExecuteAttemptAsync(context, postCommitActionQueue, async () => {
            T transactionResult;
            try {
                IDbContextTransaction transaction = isolationLevel.HasValue
                    ? await context.Database.BeginTransactionAsync(isolationLevel.Value, cancellationToken)
                    : await context.Database.BeginTransactionAsync(cancellationToken);
                await using (transaction) {
                    transactionResult = await context.Session.WithTransactionAsync(transaction.GetDbTransaction(), async () => {
                        T result = await operation(transaction.GetDbTransaction(), cancellationToken);
                        if (result is not Result { IsFailure: true } && (alwaysSave || unitOfWork.HasPendingChanges)) {
                            await unitOfWork.SaveChangesAsync(cancellationToken);
                        }

                        if (result is not Result { IsFailure: true }) {
                            await VerifiedTransactionCommit.CommitAsync(transaction, cancellationToken);
                        }
                        return result;
                    }, cancellationToken);
                }
            } catch (Exception exception) when (translateException is not null) {
                Exception translated = translateException(exception);
                if (ReferenceEquals(exception, translated)) {
                    throw;
                }
                throw translated;
            }
            return transactionResult;
        }, cancellationToken));
    }
}
