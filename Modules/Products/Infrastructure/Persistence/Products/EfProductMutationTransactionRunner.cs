using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Products.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using FoodDiary.Infrastructure.Persistence.Shared;

namespace FoodDiary.Infrastructure.Persistence.Products;

internal sealed class EfProductMutationTransactionRunner(
    FoodDiaryDbContext context,
    IUnitOfWork unitOfWork,
    IPostCommitActionQueue? postCommitActionQueue = null) : IProductMutationTransactionRunner {
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        SharedTransactionBoundary.EnsureCleanEntry(context, postCommitActionQueue);
        if (!context.Database.IsRelational()) {
            return await SharedTransactionBoundary.ExecuteAttemptAsync(context, postCommitActionQueue, async () => {
                T inMemoryResult = await operation(cancellationToken).ConfigureAwait(false);
                if (inMemoryResult is not FoodDiary.Results.Result { IsFailure: true } && unitOfWork.HasPendingChanges) {
                    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                return inMemoryResult;
            }, cancellationToken).ConfigureAwait(false);
        }

        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => SharedTransactionBoundary.ExecuteAttemptAsync(context, postCommitActionQueue, async () => {
            IDbContextTransaction transaction = await context.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
                .ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                T result = await operation(cancellationToken).ConfigureAwait(false);
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
