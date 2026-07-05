using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Common;

public interface IRecentRecipeReadService {
    Task<IReadOnlyList<RecipeModel>> GetRecentAsync(
        UserId userId,
        int limit,
        bool includePublic,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecipeOverviewReadItem>> GetRecentOverviewItemsAsync(
        UserId userId,
        int limit,
        bool includePublic,
        RecipeQueryFilters filters,
        CancellationToken cancellationToken = default);
}
