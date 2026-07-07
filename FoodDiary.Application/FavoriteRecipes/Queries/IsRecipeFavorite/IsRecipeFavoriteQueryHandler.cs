using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.FavoriteRecipes.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteRecipes.Queries.IsRecipeFavorite;

public sealed class IsRecipeFavoriteQueryHandler(
    IFavoriteRecipeReadService favoriteRecipeReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<IsRecipeFavoriteQuery, Result<bool>> {
    public async Task<Result<bool>> Handle(
        IsRecipeFavoriteQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<bool>(userIdResult);
        }

        Result<RecipeId> recipeIdResult = RequiredIdParser.Parse(
            query.RecipeId,
            nameof(query.RecipeId),
            "Recipe id must not be empty.",
            value => new RecipeId(value));
        if (recipeIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<bool, RecipeId>(recipeIdResult);
        }

        UserId userId = userIdResult.Value;
        RecipeId recipeId = recipeIdResult.Value;
        bool isFavorite = await favoriteRecipeReadService.ExistsByRecipeIdAsync(recipeId, userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(isFavorite);
    }
}
