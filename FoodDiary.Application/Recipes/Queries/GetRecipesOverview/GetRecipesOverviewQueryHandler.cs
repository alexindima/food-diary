using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.FavoriteRecipes.Mappings;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.FavoriteRecipes;

namespace FoodDiary.Application.Recipes.Queries.GetRecipesOverview;

public sealed class GetRecipesOverviewQueryHandler(
    IRecipeOverviewReadService recipeOverviewReadService,
    IRecentItemReadRepository recentItemRepository,
    IFavoriteRecipeReadRepository favoriteRecipeRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetRecipesOverviewQuery, Result<RecipeOverviewModel>> {
    private sealed record RecipeOverviewOptions(
        UserId UserId,
        int PageNumber,
        int PageSize,
        int RecentLimit,
        int FavoriteLimit,
        RecipeQueryFilters Filters);

    public async Task<Result<RecipeOverviewModel>> Handle(
        GetRecipesOverviewQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<RecipeOverviewModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<RecipeOverviewModel>(accessError);
        }

        RecipeOverviewOptions options = CreateOptions(query, userId);

        (IReadOnlyList<RecipeOverviewReadItem> items, int totalItems) = await recipeOverviewReadService.GetPagedAsync(
            options.UserId,
            query.IncludePublic,
            options.PageNumber,
            options.PageSize,
            options.Filters,
            cancellationToken).ConfigureAwait(false);

        var allRecipes = items.ToList();
        IReadOnlyList<FavoriteRecipe> allFavorites = await favoriteRecipeRepository.GetAllAsync(options.UserId, cancellationToken).ConfigureAwait(false);
        var favoriteItems = allFavorites
            .Take(options.FavoriteLimit)
            .Select(favorite => favorite.ToModel())
            .ToList();
        var favoriteLookup = allFavorites.ToDictionary(favorite => favorite.RecipeId);

        IReadOnlyList<RecipeOverviewReadItem> recentItems = await GetRecentItemsAsync(query, options, cancellationToken).ConfigureAwait(false);
        RecipeId[] favoriteRecipeIds = [.. allRecipes
            .Select(x => x.Id)
            .Concat(recentItems.Select(x => x.Id))
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

    private static RecipeOverviewOptions CreateOptions(GetRecipesOverviewQuery query, UserId userId) =>
        new(
            userId,
            Math.Max(query.Page, 1),
            Math.Max(query.Limit, 1),
            Math.Clamp(query.RecentLimit, 1, 50),
            Math.Clamp(query.FavoriteLimit, 1, 50),
            new RecipeQueryFilters(
                query.Search,
                query.Category,
                query.MaxTotalTime,
                query.CaloriesFrom,
                query.CaloriesTo,
                query.HasImage));

    private async Task<IReadOnlyList<RecipeOverviewReadItem>> GetRecentItemsAsync(
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
        IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem> recipesById = await recipeOverviewReadService.GetByIdsWithUsageAsync(
            recentIds,
            options.UserId,
            query.IncludePublic,
            cancellationToken).ConfigureAwait(false);

        return recentIds
            .Where(recipesById.ContainsKey)
            .Select(id => recipesById[id])
            .Where(item => MatchesRecentFilters(item, options.Filters))
            .ToArray();
    }

    private static bool MatchesRecentFilters(RecipeOverviewReadItem recipe, RecipeQueryFilters filters) =>
        (string.IsNullOrWhiteSpace(filters.Category) ||
         (recipe.Category?.Contains(filters.Category.Trim(), StringComparison.OrdinalIgnoreCase) ?? false)) &&
        (!filters.MaxTotalTime.HasValue || (recipe.PrepTime ?? 0) + (recipe.CookTime ?? 0) <= filters.MaxTotalTime.Value) &&
        (!filters.CaloriesFrom.HasValue || (recipe.TotalCalories ?? 0) >= filters.CaloriesFrom.Value) &&
        (!filters.CaloriesTo.HasValue || (recipe.TotalCalories ?? 0) <= filters.CaloriesTo.Value) &&
        (!filters.HasImage.HasValue || HasImage(recipe) == filters.HasImage.Value);

    private static bool HasImage(RecipeOverviewReadItem recipe) =>
        recipe.ImageUrl is not null || recipe.ImageAssetId is not null;

    private static PagedResponse<RecipeModel> CreatePagedRecipes(
        IReadOnlyList<RecipeOverviewReadItem> recipes,
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
        IEnumerable<RecipeOverviewReadItem> recipes,
        IReadOnlyDictionary<RecipeId, FavoriteRecipe> favoritesByRecipeId) =>
        [.. recipes.Select(recipe => ToRecipeModel(recipe, favoritesByRecipeId))];

    private static RecipeModel ToRecipeModel(
        RecipeOverviewReadItem recipe,
        IReadOnlyDictionary<RecipeId, FavoriteRecipe> favoritesByRecipeId) {
        FavoriteRecipe? favorite = favoritesByRecipeId.GetValueOrDefault(recipe.Id);
        return recipe.ToModel(favorite is not null, favorite?.Id.Value);
    }
}
