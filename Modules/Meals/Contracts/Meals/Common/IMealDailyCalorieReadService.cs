using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealDailyCalorieReadService {
    Task<Result<IReadOnlyList<MealDailyCalories>>> GetDailyCaloriesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);
}
