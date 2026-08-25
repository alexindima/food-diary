using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Recipes.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Infrastructure.Persistence.Recipes;

internal sealed class EfRecipeMutationTransactionRunner(
    FoodDiaryDbContext context,
    IUnitOfWork unitOfWork) : IRecipeMutationTransactionRunner {
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () => {
            IDbContextTransaction transaction = await context.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                await RecipeCompositionTransactionLock.AcquireAsync(context, cancellationToken).ConfigureAwait(false);

                T result = await operation(cancellationToken).ConfigureAwait(false);
                if (result is not FoodDiary.Results.Result { IsFailure: true } && unitOfWork.HasPendingChanges) {
                    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
        }).ConfigureAwait(false);
    }
}
