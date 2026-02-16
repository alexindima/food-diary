using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Contracts.Recipes;

namespace FoodDiary.Application.Recipes.Queries.GetRecentRecipes;

public sealed class GetRecentRecipesQueryHandler(
    IRecentItemRepository recentItemRepository,
    IRecipeRepository recipeRepository)
    : IQueryHandler<GetRecentRecipesQuery, Result<IReadOnlyList<RecipeResponse>>>
{
    public async Task<Result<IReadOnlyList<RecipeResponse>>> Handle(
        GetRecentRecipesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId is null || query.UserId == Domain.ValueObjects.UserId.Empty)
        {
            return Result.Failure<IReadOnlyList<RecipeResponse>>(Errors.Authentication.InvalidToken);
        }

        var userId = query.UserId.Value;
        var recentLimit = Math.Clamp(query.Limit, 1, 50);

        var recents = await recentItemRepository.GetRecentRecipesAsync(userId, recentLimit, cancellationToken);
        if (recents.Count == 0)
        {
            return Result.Success<IReadOnlyList<RecipeResponse>>(Array.Empty<RecipeResponse>());
        }

        var idsInOrder = recents.Select(x => x.RecipeId).ToList();
        var recipesById = await recipeRepository.GetByIdsWithUsageAsync(
            idsInOrder,
            userId,
            query.IncludePublic,
            cancellationToken);

        var response = idsInOrder
            .Where(recipesById.ContainsKey)
            .Select(id =>
            {
                var item = recipesById[id];
                return item.Recipe.ToResponse(item.UsageCount, item.Recipe.UserId == userId);
            })
            .ToList();

        return Result.Success<IReadOnlyList<RecipeResponse>>(response);
    }
}
