using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Meals.Contracts.Common;

public interface IMealDailyCalorieReadService {
    Task<Result<IReadOnlyList<MealDailyCalories>>> GetDailyCaloriesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);
}
