using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.FavoriteMeals.Common;

public interface IFavoriteMealReadModelRepository {
    Task<IReadOnlyList<FavoriteMealReadModel>> GetAllReadModelsAsync(
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
}
