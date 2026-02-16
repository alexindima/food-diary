using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Contracts.Common;
using FoodDiary.Contracts.Recipes;

namespace FoodDiary.Application.Recipes.Queries.GetRecipesWithRecent;

public sealed class GetRecipesWithRecentQueryHandler(
    IRecipeRepository recipeRepository,
    IRecentItemRepository recentItemRepository)
    : IQueryHandler<GetRecipesWithRecentQuery, Result<RecipeListWithRecentResponse>>
{
    public async Task<Result<RecipeListWithRecentResponse>> Handle(
        GetRecipesWithRecentQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId is null || query.UserId == Domain.ValueObjects.UserId.Empty)
        {
            return Result.Failure<RecipeListWithRecentResponse>(Errors.Authentication.InvalidToken);
        }

        var userId = query.UserId.Value;
        var pageNumber = Math.Max(query.Page, 1);
        var pageSize = Math.Max(query.Limit, 1);
        var recentLimit = Math.Clamp(query.RecentLimit, 1, 50);

        var (items, totalItems) = await recipeRepository.GetPagedAsync(
            userId,
            query.IncludePublic,
            pageNumber,
            pageSize,
            query.Search,
            cancellationToken);

        var allRecipes = items.Select(item => new
        {
            item.Recipe,
            item.UsageCount,
            IsOwner = item.Recipe.UserId == userId
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var allPaged = new PagedResponse<RecipeResponse>(
            allRecipes.Select(x => x.Recipe.ToResponse(x.UsageCount, x.IsOwner)).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        var recentResponses = Array.Empty<RecipeResponse>();
        if (string.IsNullOrWhiteSpace(query.Search))
        {
            var recents = await recentItemRepository.GetRecentRecipesAsync(userId, recentLimit, cancellationToken);
            if (recents.Count > 0)
            {
                var recentIds = recents.Select(x => x.RecipeId).ToList();
                var recipesById = await recipeRepository.GetByIdsWithUsageAsync(
                    recentIds,
                    userId,
                    query.IncludePublic,
                    cancellationToken);

                recentResponses = recentIds
                    .Where(recipesById.ContainsKey)
                    .Select(id =>
                    {
                        var item = recipesById[id];
                        return item.Recipe.ToResponse(item.UsageCount, item.Recipe.UserId == userId);
                    })
                    .ToArray();
            }
        }

        return Result.Success(new RecipeListWithRecentResponse(recentResponses, allPaged));
    }
}
