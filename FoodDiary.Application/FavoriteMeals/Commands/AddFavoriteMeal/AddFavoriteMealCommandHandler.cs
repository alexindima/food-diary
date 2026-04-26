using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.FavoriteMeals.Mappings;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteMeals.Commands.AddFavoriteMeal;

public class AddFavoriteMealCommandHandler(
    IFavoriteMealRepository favoriteMealRepository,
    IMealRepository mealRepository,
    IUserRepository userRepository)
    : ICommandHandler<AddFavoriteMealCommand, Result<FavoriteMealModel>> {
    public async Task<Result<FavoriteMealModel>> Handle(
        AddFavoriteMealCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<FavoriteMealModel>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<FavoriteMealModel>(accessError);
        }

        var mealId = new MealId(command.MealId);
        var meal = await mealRepository.GetByIdAsync(mealId, userId, includeItems: true, cancellationToken: cancellationToken);
        if (meal is null) {
            return Result.Failure<FavoriteMealModel>(Errors.Consumption.NotFound(command.MealId));
        }

        var existing = await favoriteMealRepository.GetByMealIdAsync(mealId, userId, cancellationToken);
        if (existing is not null) {
            return Result.Failure<FavoriteMealModel>(Errors.FavoriteMeal.AlreadyExists);
        }

        var favorite = FavoriteMeal.Create(userId, mealId, command.Name);
        await favoriteMealRepository.AddAsync(favorite, cancellationToken);

        var saved = await favoriteMealRepository.GetByIdAsync(favorite.Id, userId, cancellationToken: cancellationToken);
        return Result.Success(saved!.ToModel());
    }
}
