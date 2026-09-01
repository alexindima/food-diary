using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.RecentItems.Common;

public interface IRecentItemUsageReadService {
    Task<IReadOnlyList<RecentProductUsage>> GetRecentProductsAsync(
        UserId userId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentRecipeUsage>> GetRecentRecipesAsync(
        UserId userId,
        int limit,
        CancellationToken cancellationToken = default);
}
