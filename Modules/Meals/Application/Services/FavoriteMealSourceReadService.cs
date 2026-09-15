using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Services;

public sealed class FavoriteMealSourceReadService(IMealProjectionReadRepository mealRepository) : IFavoriteMealSourceReadService {
    public async Task<Result<FavoriteMealSourceModel>> GetAccessibleAsync(UserId userId, MealId mealId, CancellationToken cancellationToken) {
        FavoriteMealSourceModel? source = await GetAsync(userId, mealId, cancellationToken).ConfigureAwait(false);
        return source is null
            ? Result.Failure<FavoriteMealSourceModel>(MealErrors.NotFound(mealId.Value))
            : Result.Success(source);
    }
    public async Task<FavoriteMealSourceModel?> GetAsync(
        UserId userId,
        MealId mealId,
        CancellationToken cancellationToken) {
        MealProjectionReadModel? meal = await mealRepository.GetByIdMealProjectionAsync(
            mealId,
            userId,
            cancellationToken).ConfigureAwait(false);

        return meal is null
            ? null
            : new FavoriteMealSourceModel(
                meal.Date,
                meal.MealType?.ToString(),
                meal.TotalCalories,
                meal.TotalProteins,
                meal.TotalFats,
                meal.TotalCarbs,
                meal.Items.Count);
    }
}
