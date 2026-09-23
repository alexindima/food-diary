using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Application.Services;
using FoodDiary.Mediator;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadFavoriteRecipeOverview;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;

using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetRecipesOverview;

public sealed class GetRecipesOverviewQueryHandler(
    IRecipeOverviewReadService recipeOverviewReadService,
    RecentRecipeLoader recentRecipeLoader,
    ISender sender,
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
        IReadOnlyList<RecipeOverviewReadItem> recentItems = await GetRecentOverviewItemsAsync(
            options.UserId,
            options.RecentLimit,
            query.IncludePublic,
            options.Filters,
            cancellationToken).ConfigureAwait(false);
        RecipeId[] favoriteRecipeIds = [.. allRecipes
            .Select(x => x.Id)
            .Concat(recentItems.Select(x => x.Id))
            .Distinct()];
        FavoriteRecipeOverviewModel favorites = await sender.Send(new ReadFavoriteRecipeOverviewQuery(options.UserId,
            favoriteRecipeIds, options.FavoriteLimit), cancellationToken).ConfigureAwait(false);
        var favoritesByRecipeId = favorites.Items.ToDictionary(ToFavoriteRecipeId);

        PagedResponse<RecipeModel> allPaged = CreatePagedRecipes(
            allRecipes,
            favoritesByRecipeId,
            options,
            totalItems);
        RecipeModel[] recentResponses = ToRecipeModels(recentItems, favoritesByRecipeId);

        return Result.Success(new RecipeOverviewModel(recentResponses, allPaged, favorites.Preview, favorites.Total));
    }

    private static RecipeId ToFavoriteRecipeId(FavoriteRecipeModel favorite) =>
        new(favorite.RecipeId);

    private static RecipeOverviewOptions CreateOptions(GetRecipesOverviewQuery query, UserId userId) =>
        new(
            userId,
            PaginationPolicy.NormalizePage(query.Page),
            PaginationPolicy.NormalizePageSize(query.Limit, defaultPageSize: 1),
            Math.Clamp(query.RecentLimit, 1, 50),
            Math.Clamp(query.FavoriteLimit, 0, 50),
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
    private async Task<IReadOnlyList<RecipeOverviewReadItem>> GetRecentOverviewItemsAsync(
        UserId userId,
        int limit,
        bool includePublic,
        RecipeQueryFilters filters,
        CancellationToken cancellationToken = default) {
        if (!string.IsNullOrWhiteSpace(filters.Search)) {
            return [];
        }

        IReadOnlyList<RecipeOverviewReadItem> items = await recentRecipeLoader.LoadAsync(
            userId,
            limit,
            includePublic,
            cancellationToken).ConfigureAwait(false);

        return [.. items.Where(item => MatchesFilters(item, filters))];
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
