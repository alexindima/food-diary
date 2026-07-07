using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.FavoriteRecipes.Common;
using FoodDiary.Application.FavoriteRecipes.Models;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecipesOverview;

public sealed class GetRecipesOverviewQueryHandler(
    IRecipeOverviewReadService recipeOverviewReadService,
    IRecentRecipeReadService recentRecipeReadService,
    IFavoriteRecipeReadService favoriteRecipeReadService,
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
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<RecipeOverviewModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        RecipeOverviewOptions options = CreateOptions(query, userId);

        (IReadOnlyList<RecipeOverviewReadItem> items, int totalItems) = await recipeOverviewReadService.GetPagedAsync(
            options.UserId,
            query.IncludePublic,
            options.PageNumber,
            options.PageSize,
            options.Filters,
            cancellationToken).ConfigureAwait(false);

        var allRecipes = items.ToList();
        IReadOnlyList<FavoriteRecipeModel> allFavorites = await favoriteRecipeReadService.GetAllAsync(options.UserId, cancellationToken).ConfigureAwait(false);
        var favoriteItems = allFavorites
            .Take(options.FavoriteLimit)
            .ToList();
        var favoriteLookup = allFavorites.ToDictionary(favorite => new RecipeId(favorite.RecipeId));

        IReadOnlyList<RecipeOverviewReadItem> recentItems = await recentRecipeReadService.GetRecentOverviewItemsAsync(
            options.UserId,
            options.RecentLimit,
            query.IncludePublic,
            options.Filters,
            cancellationToken).ConfigureAwait(false);
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

    private static PagedResponse<RecipeModel> CreatePagedRecipes(
        IReadOnlyList<RecipeOverviewReadItem> recipes,
        IReadOnlyDictionary<RecipeId, FavoriteRecipeModel> favoritesByRecipeId,
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
        IReadOnlyDictionary<RecipeId, FavoriteRecipeModel> favoritesByRecipeId) =>
        [.. recipes.Select(recipe => ToRecipeModel(recipe, favoritesByRecipeId))];

    private static RecipeModel ToRecipeModel(
        RecipeOverviewReadItem recipe,
        IReadOnlyDictionary<RecipeId, FavoriteRecipeModel> favoritesByRecipeId) {
        FavoriteRecipeModel? favorite = favoritesByRecipeId.GetValueOrDefault(recipe.Id);
        return recipe.ToModel(favorite is not null, favorite?.Id);
    }
}
