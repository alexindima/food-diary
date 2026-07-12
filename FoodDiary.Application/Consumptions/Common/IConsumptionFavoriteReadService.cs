using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Common;

public interface IConsumptionFavoriteReadService {
    Task<IReadOnlyDictionary<MealId, FavoriteMealId>> GetFavoriteIdsByMealIdsAsync(
        UserId userId,
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<ConsumptionFavoriteMealModel> Items, int TotalItems)> GetOverviewAsync(
        UserId userId,
        int limit,
        CancellationToken cancellationToken);
}
