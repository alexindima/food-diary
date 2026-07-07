using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.FavoriteRecipes;

namespace FoodDiary.Application.FavoriteRecipes.Commands.RemoveFavoriteRecipe;

public sealed class RemoveFavoriteRecipeCommandHandler(
    IFavoriteRecipeWriteRepository favoriteRecipeRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<RemoveFavoriteRecipeCommand, Result> {
    public async Task<Result> Handle(
        RemoveFavoriteRecipeCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver
            .ResolveAsync(command.UserId, currentUserAccessService, cancellationToken)
            .ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<FavoriteRecipeId> favoriteRecipeIdResult = RequiredIdParser.Parse(
            command.FavoriteRecipeId,
            nameof(command.FavoriteRecipeId),
            "Favorite recipe id must not be empty.",
            value => new FavoriteRecipeId(value));
        if (favoriteRecipeIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(favoriteRecipeIdResult);
        }

        FavoriteRecipeId favoriteRecipeId = favoriteRecipeIdResult.Value;
        FavoriteRecipe? favorite = await favoriteRecipeRepository.GetByIdAsync(
            favoriteRecipeId, userId, asTracking: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (favorite is null) {
            return Result.Failure(Errors.FavoriteRecipe.NotFound(command.FavoriteRecipeId));
        }

        await favoriteRecipeRepository.DeleteAsync(favorite, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
