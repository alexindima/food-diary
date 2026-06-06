using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.FavoriteRecipes.Mappings;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Recipe = FoodDiary.Domain.Entities.Recipes.Recipe;
using FoodDiary.Domain.Entities.FavoriteRecipes;

namespace FoodDiary.Application.Recipes.Queries.GetRecipesOverview;

public sealed class GetRecipesOverviewQueryHandler(
    IRecipeRepository recipeRepository,
    IRecentItemRepository recentItemRepository,
    IFavoriteRecipeRepository favoriteRecipeRepository)
    : IQueryHandler<GetRecipesOverviewQuery, Result<RecipeOverviewModel>> {
    private sealed record RecipeOverviewOptions(
        UserId UserId,
        int PageNumber,
        int PageSize,
        int RecentLimit,
        int FavoriteLimit);

    private sealed record RecipeListItem(
        Recipe Recipe,
        int UsageCount,
        bool IsOwner);

    public async Task<Result<RecipeOverviewModel>> Handle(
        GetRecipesOverviewQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<RecipeOverviewModel>(Errors.Authentication.InvalidToken);
        }

        RecipeOverviewOptions options = CreateOptions(query);

        (IReadOnlyList<(Recipe Recipe, int UsageCount)> items, int totalItems) = await recipeRepository.GetPagedAsync(
            options.UserId,
            query.IncludePublic,
            options.PageNumber,
            options.PageSize,
            query.Search,
            cancellationToken).ConfigureAwait(false);

        var allRecipes = items
            .Select(item => ToListItem(item.Recipe, item.UsageCount, options.UserId))
            .ToList();
        IReadOnlyList<FavoriteRecipe> allFavorites = await favoriteRecipeRepository.GetAllAsync(options.UserId, cancellationToken).ConfigureAwait(false);
        var favoriteItems = allFavorites
            .Take(options.FavoriteLimit)
            .Select(favorite => favorite.ToModel())
            .ToList();
        var favoriteLookup = allFavorites.ToDictionary(favorite => favorite.RecipeId);

        IReadOnlyList<RecipeListItem> recentItems = await GetRecentItemsAsync(query, options, cancellationToken).ConfigureAwait(false);
        RecipeId[] favoriteRecipeIds = [.. allRecipes
            .Select(x => x.Recipe.Id)
            .Concat(recentItems.Select(x => x.Recipe.Id))
            .Distinct()];
        var favoritesByRecipeId = favoriteLookup
            .Where(pair => favoriteRecipeIds.Contains(pair.Key))
            .ToDictionary();

        PagedResponse<RecipeModel> allPaged = CreatePagedRecipes(
            allRecipes,
            favoritesByRecipeId,
            options,
            totalItems);
        RecipeModel[] recentResponses = ToRecipeModels(recentItems, favoritesByRecipeId);

        return Result.Success(new RecipeOverviewModel(recentResponses, allPaged, favoriteItems, allFavorites.Count));
    }

    private static RecipeOverviewOptions CreateOptions(GetRecipesOverviewQuery query) =>
        new(
            new UserId(query.UserId!.Value),
            Math.Max(query.Page, 1),
            Math.Max(query.Limit, 1),
            Math.Clamp(query.RecentLimit, 1, 50),
            Math.Clamp(query.FavoriteLimit, 1, 50));

    private async Task<IReadOnlyList<RecipeListItem>> GetRecentItemsAsync(
        GetRecipesOverviewQuery query,
        RecipeOverviewOptions options,
        CancellationToken cancellationToken) {
        if (!string.IsNullOrWhiteSpace(query.Search)) {
            return [];
        }

        IReadOnlyList<RecentRecipeUsage> recents = await recentItemRepository.GetRecentRecipesAsync(
            options.UserId,
            options.RecentLimit,
            cancellationToken).ConfigureAwait(false);
        if (recents.Count == 0) {
            return [];
        }

        var recentIds = recents.Select(x => x.RecipeId).ToList();
        IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)> recipesById = await recipeRepository.GetByIdsWithUsageAsync(
            recentIds,
            options.UserId,
            query.IncludePublic,
            cancellationToken).ConfigureAwait(false);

        return recentIds
            .Where(recipesById.ContainsKey)
            .Select(id => recipesById[id])
            .Select(item => ToListItem(item.Recipe, item.UsageCount, options.UserId))
            .ToArray();
    }

    private static RecipeListItem ToListItem(Recipe recipe, int usageCount, UserId userId) =>
        new(recipe, usageCount, recipe.UserId == userId);

    private static PagedResponse<RecipeModel> CreatePagedRecipes(
        IReadOnlyList<RecipeListItem> recipes,
        IReadOnlyDictionary<RecipeId, FavoriteRecipe> favoritesByRecipeId,
        RecipeOverviewOptions options,
        int totalItems) =>
        new(
            ToRecipeModels(recipes, favoritesByRecipeId).ToList(),
            options.PageNumber,
            options.PageSize,
            (int)Math.Ceiling(totalItems / (double)options.PageSize),
            totalItems);

    private static RecipeModel[] ToRecipeModels(
        IEnumerable<RecipeListItem> recipes,
        IReadOnlyDictionary<RecipeId, FavoriteRecipe> favoritesByRecipeId) =>
        [.. recipes.Select(recipe => ToRecipeModel(recipe, favoritesByRecipeId))];

    private static RecipeModel ToRecipeModel(
        RecipeListItem recipe,
        IReadOnlyDictionary<RecipeId, FavoriteRecipe> favoritesByRecipeId) {
        FavoriteRecipe? favorite = favoritesByRecipeId.GetValueOrDefault(recipe.Recipe.Id);
        return recipe.Recipe.ToModel(
            recipe.UsageCount,
            recipe.IsOwner,
            favorite is not null,
            favorite?.Id.Value);
    }
}
