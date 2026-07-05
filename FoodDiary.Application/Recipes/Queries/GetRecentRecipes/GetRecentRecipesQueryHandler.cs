using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecentRecipes;

public sealed class GetRecentRecipesQueryHandler(IRecentRecipeReadService recentRecipeReadService)
    : IQueryHandler<GetRecentRecipesQuery, Result<IReadOnlyList<RecipeModel>>> {
    public async Task<Result<IReadOnlyList<RecipeModel>>> Handle(
        GetRecentRecipesQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<RecipeModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        int recentLimit = Math.Clamp(query.Limit, 1, 50);

        IReadOnlyList<RecipeModel> response = await recentRecipeReadService.GetRecentAsync(
            userId,
            recentLimit,
            query.IncludePublic,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(response);
    }
}
