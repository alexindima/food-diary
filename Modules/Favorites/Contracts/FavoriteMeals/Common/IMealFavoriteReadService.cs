using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.FavoriteMeals.Common;

public interface IMealFavoriteReadService {
    Task<IReadOnlyDictionary<MealId, FavoriteMealId>> GetFavoriteIdsByMealIdsAsync(
        UserId userId,
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<MealFavoriteMealModel> Items, int TotalItems)> GetOverviewAsync(
        UserId userId,
        int limit,
        CancellationToken cancellationToken);
}
