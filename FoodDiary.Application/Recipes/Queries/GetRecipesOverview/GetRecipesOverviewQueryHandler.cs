using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.FavoriteRecipes.Common;
using FoodDiary.Application.FavoriteRecipes.Mappings;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.RecentItems.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecipesOverview;

public sealed class GetRecipesOverviewQueryHandler(
    IRecipeRepository recipeRepository,
    IRecentItemRepository recentItemRepository,
    IFavoriteRecipeRepository favoriteRecipeRepository)
    : IQueryHandler<GetRecipesOverviewQuery, Result<RecipeOverviewModel>> {
    public async Task<Result<RecipeOverviewModel>> Handle(
        GetRecipesOverviewQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<RecipeOverviewModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var pageNumber = Math.Max(query.Page, 1);
        var pageSize = Math.Max(query.Limit, 1);
        var recentLimit = Math.Clamp(query.RecentLimit, 1, 50);
        var favoriteLimit = Math.Clamp(query.FavoriteLimit, 1, 50);

        var (items, totalItems) = await recipeRepository.GetPagedAsync(
            userId,
            query.IncludePublic,
            pageNumber,
            pageSize,
            query.Search,
            cancellationToken);

        var allRecipes = items.Select(item => new {
            item.Recipe,
            item.UsageCount,
            IsOwner = item.Recipe.UserId == userId
        }).ToList();
        var allFavorites = await favoriteRecipeRepository.GetAllAsync(userId, cancellationToken);
        var favoriteItems = allFavorites
            .Take(favoriteLimit)
            .Select(favorite => favorite.ToModel())
            .ToList();
        var favoriteLookup = allFavorites.ToDictionary(favorite => favorite.RecipeId);
        var recentItems = Array.Empty<(Domain.Entities.Recipes.Recipe Recipe, int UsageCount, bool IsOwner)>();

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var recentResponses = Array.Empty<RecipeModel>();
        if (string.IsNullOrWhiteSpace(query.Search)) {
            var recents = await recentItemRepository.GetRecentRecipesAsync(userId, recentLimit, cancellationToken);
            if (recents.Count > 0) {
                var recentIds = recents.Select(x => x.RecipeId).ToList();
                var recipesById = await recipeRepository.GetByIdsWithUsageAsync(
                    recentIds,
                    userId,
                    query.IncludePublic,
                    cancellationToken);

                recentItems = recentIds
                    .Where(recipesById.ContainsKey)
                    .Select(id => {
                        var item = recipesById[id];
                        return (item.Recipe, item.UsageCount, item.Recipe.UserId == userId);
                    })
                    .ToArray();
            }
        }

        var favoriteRecipeIds = allRecipes
            .Select(x => x.Recipe.Id)
            .Concat(recentItems.Select(x => x.Recipe.Id))
            .Distinct()
            .ToArray();
        var favoritesByRecipeId = favoriteLookup
            .Where(pair => favoriteRecipeIds.Contains(pair.Key))
            .ToDictionary();

        var allPaged = new PagedResponse<RecipeModel>(
            allRecipes.Select(x => {
                var favorite = favoritesByRecipeId.GetValueOrDefault(x.Recipe.Id);
                return x.Recipe.ToModel(
                    x.UsageCount,
                    x.IsOwner,
                    favorite is not null,
                    favorite?.Id.Value);
            }).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        recentResponses = recentItems
            .Select(x => {
                var favorite = favoritesByRecipeId.GetValueOrDefault(x.Recipe.Id);
                return x.Recipe.ToModel(
                    x.UsageCount,
                    x.IsOwner,
                    favorite is not null,
                    favorite?.Id.Value);
            })
            .ToArray();

        return Result.Success(new RecipeOverviewModel(recentResponses, allPaged, favoriteItems, allFavorites.Count));
    }
}
