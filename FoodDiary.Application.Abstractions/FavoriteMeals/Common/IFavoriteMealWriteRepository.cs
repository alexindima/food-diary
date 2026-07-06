using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.FavoriteMeals.Common;

public interface IFavoriteMealWriteRepository {
    Task<FavoriteMeal?> GetByIdAsync(
        FavoriteMealId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<FavoriteMeal?> GetByMealIdAsync(
        MealId mealId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<FavoriteMeal> AddAsync(FavoriteMeal favorite, CancellationToken cancellationToken = default);

    Task DeleteAsync(FavoriteMeal favorite, CancellationToken cancellationToken = default);
}
