using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.FavoriteMeals.Mappings;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.FavoriteMeals.Commands.AddFavoriteMeal;

public class AddFavoriteMealCommandHandler(
    IFavoriteMealWriteRepository favoriteMealRepository,
    IMealReadRepository mealRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<AddFavoriteMealCommand, Result<FavoriteMealModel>> {
    public async Task<Result<FavoriteMealModel>> Handle(
        AddFavoriteMealCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<FavoriteMealModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<FavoriteMealModel>(accessError);
        }

        var mealId = new MealId(command.MealId);
        Meal? meal = await mealRepository.GetByIdAsync(mealId, userId, includeItems: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (meal is null) {
            return Result.Failure<FavoriteMealModel>(Errors.Consumption.NotFound(command.MealId));
        }

        FavoriteMeal? existing = await favoriteMealRepository.GetByMealIdAsync(mealId, userId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            return Result.Failure<FavoriteMealModel>(Errors.FavoriteMeal.AlreadyExists);
        }

        var favorite = FavoriteMeal.Create(userId, mealId, command.Name);
        await favoriteMealRepository.AddAsync(favorite, cancellationToken).ConfigureAwait(false);

        return Result.Success(favorite.ToModel(meal));
    }
}
