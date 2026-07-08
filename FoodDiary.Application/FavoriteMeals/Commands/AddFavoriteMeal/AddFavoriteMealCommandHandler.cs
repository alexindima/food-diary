using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.FavoriteMeals.Mappings;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.FavoriteMeals.Commands.AddFavoriteMeal;

public sealed class AddFavoriteMealCommandHandler(
    IFavoriteMealWriteRepository favoriteMealRepository,
    IMealReadRepository mealRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<AddFavoriteMealCommand, Result<FavoriteMealModel>> {
    public async Task<Result<FavoriteMealModel>> Handle(
        AddFavoriteMealCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver
            .ResolveAsync(command.UserId, currentUserAccessService, cancellationToken)
            .ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<FavoriteMealModel>(userIdResult);
        }

        Result<MealId> mealIdResult = RequiredIdParser.Parse(
            command.MealId,
            nameof(command.MealId),
            "Meal id must not be empty.",
            value => new MealId(value));
        if (mealIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<FavoriteMealModel, MealId>(mealIdResult);
        }

        UserId userId = userIdResult.Value;
        MealId mealId = mealIdResult.Value;
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
