using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteMeals.Common;

public interface IFavoriteMealRepository {
    Task<FavoriteMeal> AddAsync(FavoriteMeal favorite, CancellationToken cancellationToken = default);

    Task DeleteAsync(FavoriteMeal favorite, CancellationToken cancellationToken = default);

    Task<FavoriteMeal?> GetByIdAsync(
        FavoriteMealId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<FavoriteMeal?> GetByMealIdAsync(
        MealId mealId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<MealId, FavoriteMeal>> GetByMealIdsAsync(
        UserId userId,
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FavoriteMeal>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
