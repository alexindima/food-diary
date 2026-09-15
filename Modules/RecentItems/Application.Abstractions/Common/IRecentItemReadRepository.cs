using FoodDiary.Domain.ValueObjects.Ids;

using FoodDiary.Modules.RecentItems.Contracts.Common;

namespace FoodDiary.Modules.RecentItems.Application.Abstractions.Common;

public interface IRecentItemReadRepository {
    Task<IReadOnlyList<RecentProductUsage>> GetRecentProductsAsync(
        UserId userId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentRecipeUsage>> GetRecentRecipesAsync(
        UserId userId,
        int limit,
        CancellationToken cancellationToken = default);
}
