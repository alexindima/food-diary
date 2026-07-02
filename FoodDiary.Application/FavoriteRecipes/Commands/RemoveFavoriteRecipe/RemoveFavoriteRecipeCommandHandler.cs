using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.FavoriteRecipes;

namespace FoodDiary.Application.FavoriteRecipes.Commands.RemoveFavoriteRecipe;

public class RemoveFavoriteRecipeCommandHandler(
    IFavoriteRecipeRepository favoriteRecipeRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<RemoveFavoriteRecipeCommand, Result> {
    public async Task<Result> Handle(
        RemoveFavoriteRecipeCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var favoriteRecipeId = new FavoriteRecipeId(command.FavoriteRecipeId);
        FavoriteRecipe? favorite = await favoriteRecipeRepository.GetByIdAsync(
            favoriteRecipeId, userId, asTracking: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (favorite is null) {
            return Result.Failure(Errors.FavoriteRecipe.NotFound(command.FavoriteRecipeId));
        }

        await favoriteRecipeRepository.DeleteAsync(favorite, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
