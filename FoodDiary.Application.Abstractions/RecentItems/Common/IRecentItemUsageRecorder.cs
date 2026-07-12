using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.RecentItems.Common;

public interface IRecentItemUsageRecorder {
    Task RegisterUsageAsync(
        UserId userId,
        IReadOnlyCollection<ProductId> productIds,
        IReadOnlyCollection<RecipeId> recipeIds,
        CancellationToken cancellationToken = default);
}
