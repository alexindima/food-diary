using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.RecentItems.Common;

public interface IRecentItemWriteRepository {
    Task RegisterUsageAsync(
        UserId userId,
        IReadOnlyCollection<ProductId> productIds,
        IReadOnlyCollection<RecipeId> recipeIds,
        CancellationToken cancellationToken = default);
}
