using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadMealNutritionStatistics;
using FoodDiary.Results;

namespace FoodDiary.Modules.Meals.Application.Queries.ReadMealNutritionStatistics;

public sealed class ReadMealNutritionStatisticsQueryHandler(IMealNutritionStatisticsReadService statistics)
    : IQueryHandler<ReadMealNutritionStatisticsQuery, Result<IReadOnlyList<MealNutritionStatisticsBucket>>> {
    public Task<Result<IReadOnlyList<MealNutritionStatisticsBucket>>> Handle(
        ReadMealNutritionStatisticsQuery query, CancellationToken cancellationToken) {
        if (!LocalCalendar.TryResolve(query.TimeZoneId, offsetMinutes: null, out TimeZoneInfo zone)) {
            return Task.FromResult(Result.Failure<IReadOnlyList<MealNutritionStatisticsBucket>>(
                Errors.Validation.Invalid(nameof(query.TimeZoneId), "Provide a valid calendar time zone.")));
        }

        return statistics.GetStatisticsAsync(query.UserId, query.DateFrom, query.DateTo, query.QuantizationDays,
            cancellationToken, query.TimeZoneId is null ? null : zone);
    }
}
