using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.RecentItems.Application.Abstractions.Common;
using FoodDiary.Modules.RecentItems.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.RecentItems.Infrastructure.Persistence.RecentItems;

public sealed class PostCommitRecentItemUsageRecorder(
    IRecentItemWriteRepository repository,
    IPostCommitActionQueue postCommitActionQueue,
    IUnitOfWork unitOfWork) : IRecentItemUsageRecorder {
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
