using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteMeals.Common;

public interface IFavoriteMealReadService {
    Task<IReadOnlyList<FavoriteMealModel>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByMealIdAsync(
        MealId mealId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<MealId, FavoriteMealId>> GetFavoriteIdsByMealIdsAsync(
        UserId userId,
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<FavoriteMealModel> Items, int TotalItems)> GetOverviewAsync(
        UserId userId,
        int limit,
        CancellationToken cancellationToken = default);
}
