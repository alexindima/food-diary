using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.RecentItems;

public sealed class PostCommitRecentItemUsageRecorder(
    IRecentItemWriteRepository repository,
    IPostCommitActionQueue postCommitActionQueue,
    IUnitOfWork unitOfWork) : IRecentItemUsageRecorder {
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public Task RegisterUsageAsync(
        UserId userId,
        IReadOnlyCollection<ProductId> productIds,
        IReadOnlyCollection<RecipeId> recipeIds,
        CancellationToken cancellationToken = default) {
        if (productIds.Count == 0 && recipeIds.Count == 0) {
            return Task.CompletedTask;
        }

        ProductId[] capturedProductIds = [.. productIds];
        RecipeId[] capturedRecipeIds = [.. recipeIds];
        postCommitActionQueue.Enqueue(
            "recent-items.register-usage",
            async token => {
                await repository.RegisterUsageAsync(userId, capturedProductIds, capturedRecipeIds, token).ConfigureAwait(false);
                if (unitOfWork.HasPendingChanges) {
                    await unitOfWork.SaveChangesAsync(token).ConfigureAwait(false);
                }
            });
        return Task.CompletedTask;
    }
}
