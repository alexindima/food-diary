using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.Recipes.Queries.GetRecentRecipes;

public sealed class GetRecentRecipesQueryHandler(
    IRecentItemReadRepository recentItemRepository,
    IRecipeReadRepository recipeRepository)
    : IQueryHandler<GetRecentRecipesQuery, Result<IReadOnlyList<RecipeModel>>> {
    public async Task<Result<IReadOnlyList<RecipeModel>>> Handle(
        GetRecentRecipesQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<RecipeModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        int recentLimit = Math.Clamp(query.Limit, 1, 50);

        IReadOnlyList<RecentRecipeUsage> recents = await recentItemRepository.GetRecentRecipesAsync(userId, recentLimit, cancellationToken).ConfigureAwait(false);
        if (recents.Count == 0) {
            return Result.Success<IReadOnlyList<RecipeModel>>(Array.Empty<RecipeModel>());
        }

        var idsInOrder = recents.Select(x => x.RecipeId).ToList();
        IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)> recipesById = await recipeRepository.GetByIdsWithUsageAsync(
            idsInOrder,
            userId,
            query.IncludePublic,
            cancellationToken).ConfigureAwait(false);

        var response = idsInOrder
            .Where(recipesById.ContainsKey)
            .Select(id => {
                (Recipe Recipe, int UsageCount) item = recipesById[id];
                return item.Recipe.ToModel(item.UsageCount, item.Recipe.UserId == userId);
            })
            .ToList();

        return Result.Success<IReadOnlyList<RecipeModel>>(response);
    }
}
