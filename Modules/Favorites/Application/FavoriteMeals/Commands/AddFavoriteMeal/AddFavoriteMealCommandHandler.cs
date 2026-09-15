using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Mappings;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteMeals;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Commands.AddFavoriteMeal;

public sealed class AddFavoriteMealCommandHandler(
    IFavoriteMealWriteRepository favoriteMealRepository,
    IFavoriteMealSourceReadService favoriteMealSourceReadService,
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
        Result<FavoriteMealSourceModel> source = await favoriteMealSourceReadService
            .GetAccessibleAsync(userId, mealId, cancellationToken)
            .ConfigureAwait(false);
        if (source.IsFailure) {
            return Result.Failure<FavoriteMealModel>(source.Error);
        }

        FavoriteMeal? existing = await favoriteMealRepository.GetByMealIdAsync(mealId, userId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            return Result.Failure<FavoriteMealModel>(FavoriteMealErrors.AlreadyExists);
        }

        var favorite = FavoriteMeal.Create(userId, mealId, command.Name);
        await favoriteMealRepository.AddAsync(favorite, cancellationToken).ConfigureAwait(false);

        return Result.Success(favorite.ToModel(source.Value));
    }
}
