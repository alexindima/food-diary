using FoodDiary.Results;
using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.FavoriteMeals.Common;

public interface IFavoriteMealSourceReadService {
    Task<Result<FavoriteMealSourceModel>> GetAccessibleAsync(
        UserId userId,
        MealId mealId,
        CancellationToken cancellationToken);
}
