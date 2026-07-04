using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.Users.Common;
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
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

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
