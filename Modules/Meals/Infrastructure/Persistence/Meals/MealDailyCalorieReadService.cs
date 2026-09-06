using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Infrastructure.Persistence.Meals;

internal sealed class MealDailyCalorieReadService(IMealNutritionStatisticsReadService nutrition) : IMealDailyCalorieReadService {
    public async Task<Result<IReadOnlyList<MealDailyCalories>>> GetDailyCaloriesAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default) {
        Result<IReadOnlyList<MealNutritionStatisticsBucket>> result = await nutrition.GetStatisticsAsync(userId, dateFrom, dateTo, quantizationDays: 1, cancellationToken).ConfigureAwait(false);
        return result.IsFailure ? Result.Failure<IReadOnlyList<MealDailyCalories>>(result.Error)
            : Result.Success<IReadOnlyList<MealDailyCalories>>([.. result.Value.Select(bucket => new MealDailyCalories(bucket.DateFrom, bucket.TotalCalories))]);
    }
}
