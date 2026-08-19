using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.RecentItems;

public sealed class PostCommitRecentItemUsageRecorder(
    IRecentItemWriteRepository repository,
    IPostCommitActionQueue postCommitActionQueue) : IRecentItemUsageRecorder {
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
            token => repository.RegisterUsageAsync(userId, capturedProductIds, capturedRecipeIds, token));
        return Task.CompletedTask;
    }
}
