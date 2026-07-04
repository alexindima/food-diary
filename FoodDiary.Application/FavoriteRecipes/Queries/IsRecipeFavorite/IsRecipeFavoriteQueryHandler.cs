using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.FavoriteRecipes;

namespace FoodDiary.Application.FavoriteRecipes.Queries.IsRecipeFavorite;

public class IsRecipeFavoriteQueryHandler(
    IFavoriteRecipeReadRepository favoriteRecipeRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<IsRecipeFavoriteQuery, Result<bool>> {
    public async Task<Result<bool>> Handle(
        IsRecipeFavoriteQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<bool>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<bool>(accessError);
        }

        var recipeId = new RecipeId(query.RecipeId);
        FavoriteRecipe? favorite = await favoriteRecipeRepository.GetByRecipeIdAsync(recipeId, userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(favorite is not null);
    }
}
