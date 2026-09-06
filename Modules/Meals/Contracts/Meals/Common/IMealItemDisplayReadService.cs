using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealItemDisplayReadService {
    Task<IReadOnlyList<MealItemDisplayReadModel>> GetByMealIdsAsync(
        UserId userId, IReadOnlyCollection<MealId> mealIds, CancellationToken cancellationToken = default);
}
