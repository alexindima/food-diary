using FoodDiary.Results;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Contracts.Common;

public interface IMealNutritionStatisticsReadService {
    Task<Result<IReadOnlyList<MealNutritionStatisticsBucket>>> GetStatisticsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays,
        CancellationToken cancellationToken = default);
}
