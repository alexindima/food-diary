using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteMeals.Commands.RemoveFavoriteMeal;

public class RemoveFavoriteMealCommandHandler(
    IFavoriteMealRepository favoriteMealRepository,
    IUserRepository userRepository)
    : ICommandHandler<RemoveFavoriteMealCommand, Result> {
    public async Task<Result> Handle(
        RemoveFavoriteMealCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var favoriteMealId = new FavoriteMealId(command.FavoriteMealId);
        var favorite = await favoriteMealRepository.GetByIdAsync(
            favoriteMealId, userId, asTracking: true, cancellationToken: cancellationToken);

        if (favorite is null) {
            return Result.Failure(Errors.FavoriteMeal.NotFound(command.FavoriteMealId));
        }

        await favoriteMealRepository.DeleteAsync(favorite, cancellationToken);
        return Result.Success();
    }
}
