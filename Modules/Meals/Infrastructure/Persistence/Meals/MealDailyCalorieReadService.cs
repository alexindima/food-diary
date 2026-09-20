using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Meals.Infrastructure.Persistence.Meals;

internal sealed class MealDailyCalorieReadService(IMealNutritionStatisticsReadService nutrition) : IMealDailyCalorieReadService {
    public async Task<Result<IReadOnlyList<MealDailyCalories>>> GetDailyCaloriesAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default, TimeZoneInfo? timeZone = null) {
        Result<IReadOnlyList<MealNutritionStatisticsBucket>> result = await nutrition.GetStatisticsAsync(userId, dateFrom, dateTo, quantizationDays: 1, cancellationToken, timeZone).ConfigureAwait(false);
        return result.IsFailure ? Result.Failure<IReadOnlyList<MealDailyCalories>>(result.Error)
            : Result.Success<IReadOnlyList<MealDailyCalories>>([.. result.Value.Select(bucket => new MealDailyCalories(timeZone is null ? bucket.DateFrom : LocalCalendar.DateAt(bucket.DateFrom, timeZone).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), bucket.TotalCalories))]);
    }
}
