using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Services;

public sealed class RecentRecipeReadService(
    IRecentItemReadRepository recentItemRepository,
    IRecipeOverviewReadService recipeOverviewReadService)
    : IRecentRecipeReadService {
    public async Task<IReadOnlyList<RecipeModel>> GetRecentAsync(
        UserId userId,
        int limit,
        bool includePublic,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<RecipeOverviewReadItem> items = await GetRecentItemsAsync(
            userId,
            limit,
            includePublic,
            cancellationToken).ConfigureAwait(false);

        return [.. items.Select(item => item.ToModel())];
    }

    public async Task<IReadOnlyList<RecipeOverviewReadItem>> GetRecentOverviewItemsAsync(
        UserId userId,
        int limit,
        bool includePublic,
        RecipeQueryFilters filters,
        CancellationToken cancellationToken = default) {
        if (!string.IsNullOrWhiteSpace(filters.Search)) {
            return [];
        }

        IReadOnlyList<RecipeOverviewReadItem> items = await GetRecentItemsAsync(
            userId,
            limit,
            includePublic,
            cancellationToken).ConfigureAwait(false);

        return [.. items.Where(item => MatchesFilters(item, filters))];
    }

    private async Task<IReadOnlyList<RecipeOverviewReadItem>> GetRecentItemsAsync(
        UserId userId,
        int limit,
        bool includePublic,
        CancellationToken cancellationToken) {
        IReadOnlyList<RecentRecipeUsage> recents = await recentItemRepository.GetRecentRecipesAsync(
            userId,
            limit,
            cancellationToken).ConfigureAwait(false);
        if (recents.Count == 0) {
            return [];
        }

        RecipeId[] idsInOrder = [.. recents.Select(recent => recent.RecipeId)];
        IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem> recipesById = await recipeOverviewReadService.GetByIdsWithUsageAsync(
            idsInOrder,
            userId,
            includePublic,
            cancellationToken).ConfigureAwait(false);

        return [.. idsInOrder
            .Where(recipesById.ContainsKey)
            .Select(id => recipesById[id])];
    }

    private static bool MatchesFilters(RecipeOverviewReadItem recipe, RecipeQueryFilters filters) =>
        (string.IsNullOrWhiteSpace(filters.Category) ||
         (recipe.Category?.Contains(filters.Category.Trim(), StringComparison.OrdinalIgnoreCase) ?? false)) &&
        (!filters.MaxTotalTime.HasValue || (recipe.PrepTime ?? 0) + (recipe.CookTime ?? 0) <= filters.MaxTotalTime.Value) &&
        (!filters.CaloriesFrom.HasValue || (recipe.TotalCalories ?? 0) >= filters.CaloriesFrom.Value) &&
        (!filters.CaloriesTo.HasValue || (recipe.TotalCalories ?? 0) <= filters.CaloriesTo.Value) &&
        (!filters.HasImage.HasValue || HasImage(recipe) == filters.HasImage.Value);

    private static bool HasImage(RecipeOverviewReadItem recipe) =>
        recipe.ImageUrl is not null || recipe.ImageAssetId is not null;
}
