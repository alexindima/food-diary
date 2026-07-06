using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.FavoriteMeals.Common;

public interface IFavoriteMealReadRepository {
    Task<FavoriteMeal?> GetByIdAsync(
        FavoriteMealId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<FavoriteMeal?> GetByMealIdAsync(
        MealId mealId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByMealIdAsync(
        MealId mealId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<MealId, FavoriteMeal>> GetByMealIdsAsync(
        UserId userId,
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<MealId, FavoriteMealId>> GetFavoriteIdsByMealIdsAsync(
        UserId userId,
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FavoriteMeal>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
