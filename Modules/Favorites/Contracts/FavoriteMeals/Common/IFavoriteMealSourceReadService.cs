using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Common;

public interface IFavoriteMealSourceReadService {
    Task<Result<FavoriteMealSourceModel>> GetAccessibleAsync(
        UserId userId,
        MealId mealId,
        CancellationToken cancellationToken);
}
