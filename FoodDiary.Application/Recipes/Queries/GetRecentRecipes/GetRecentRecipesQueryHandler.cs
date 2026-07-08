using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecentRecipes;

public sealed class GetRecentRecipesQueryHandler(IRecentRecipeReadService recentRecipeReadService)
    : IQueryHandler<GetRecentRecipesQuery, Result<IReadOnlyList<RecipeModel>>> {
    public async Task<Result<IReadOnlyList<RecipeModel>>> Handle(
        GetRecentRecipesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<IReadOnlyList<RecipeModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        int recentLimit = Math.Clamp(query.Limit, 1, 50);

        IReadOnlyList<RecipeModel> response = await recentRecipeReadService.GetRecentAsync(
            userId,
            recentLimit,
            query.IncludePublic,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(response);
    }
}
