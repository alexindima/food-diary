using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.FavoriteMeals.Common;

public interface IFavoriteMealSourceReadService {
    Task<FavoriteMealSourceModel?> GetAsync(
        UserId userId,
        MealId mealId,
        CancellationToken cancellationToken);
}
