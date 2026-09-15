using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Contracts.Common;

public interface IMealItemDisplayReadService {
    Task<IReadOnlyList<MealItemDisplayReadModel>> GetByMealIdsAsync(
        UserId userId, IReadOnlyCollection<MealId> mealIds, CancellationToken cancellationToken = default);
}
