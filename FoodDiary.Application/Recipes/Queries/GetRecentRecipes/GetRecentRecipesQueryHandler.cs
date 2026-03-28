using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.RecentItems.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecentRecipes;

public sealed class GetRecentRecipesQueryHandler(
    IRecentItemRepository recentItemRepository,
    IRecipeRepository recipeRepository)
    : IQueryHandler<GetRecentRecipesQuery, Result<IReadOnlyList<RecipeModel>>> {
    public async Task<Result<IReadOnlyList<RecipeModel>>> Handle(
        GetRecentRecipesQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<RecipeModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var recentLimit = Math.Clamp(query.Limit, 1, 50);

        var recents = await recentItemRepository.GetRecentRecipesAsync(userId, recentLimit, cancellationToken);
        if (recents.Count == 0) {
            return Result.Success<IReadOnlyList<RecipeModel>>(Array.Empty<RecipeModel>());
        }

        var idsInOrder = recents.Select(x => x.RecipeId).ToList();
        var recipesById = await recipeRepository.GetByIdsWithUsageAsync(
            idsInOrder,
            userId,
            query.IncludePublic,
            cancellationToken);

        var response = idsInOrder
            .Where(recipesById.ContainsKey)
            .Select(id => {
                var item = recipesById[id];
                return item.Recipe.ToModel(item.UsageCount, item.Recipe.UserId == userId);
            })
            .ToList();

        return Result.Success<IReadOnlyList<RecipeModel>>(response);
    }
}
