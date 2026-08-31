using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Recipes.Common;

public interface IRecipeOverviewReadService {
    Task<(IReadOnlyList<RecipeOverviewReadItem> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        RecipeQueryFilters filters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem>> GetByIdsWithUsageAsync(
        IEnumerable<RecipeId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<RecipeOverviewReadItem> Items, int TotalItems)> GetExplorePagedAsync(
        UserId currentUserId,
        int page,
        int limit,
        string? search,
        string? category,
        int? maxPrepTime,
        string sortBy,
        CancellationToken cancellationToken = default);
}
