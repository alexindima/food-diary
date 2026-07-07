using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.FavoriteMeals;

namespace FoodDiary.Application.FavoriteMeals.Commands.RemoveFavoriteMeal;

public sealed class RemoveFavoriteMealCommandHandler(
    IFavoriteMealWriteRepository favoriteMealRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<RemoveFavoriteMealCommand, Result> {
    public async Task<Result> Handle(
        RemoveFavoriteMealCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver
            .ResolveAsync(command.UserId, currentUserAccessService, cancellationToken)
            .ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        var favoriteMealId = new FavoriteMealId(command.FavoriteMealId);
        FavoriteMeal? favorite = await favoriteMealRepository.GetByIdAsync(
            favoriteMealId, userId, asTracking: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (favorite is null) {
            return Result.Failure(Errors.FavoriteMeal.NotFound(command.FavoriteMealId));
        }

        await favoriteMealRepository.DeleteAsync(favorite, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
