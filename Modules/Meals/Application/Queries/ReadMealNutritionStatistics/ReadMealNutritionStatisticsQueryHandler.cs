using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadMealNutritionStatistics;
using FoodDiary.Results;

namespace FoodDiary.Modules.Meals.Application.Queries.ReadMealNutritionStatistics;

public sealed class ReadMealNutritionStatisticsQueryHandler(IMealNutritionStatisticsReadService statistics)
    : IQueryHandler<ReadMealNutritionStatisticsQuery, Result<IReadOnlyList<MealNutritionStatisticsBucket>>> {
    public Task<Result<IReadOnlyList<MealNutritionStatisticsBucket>>> Handle(
        ReadMealNutritionStatisticsQuery query, CancellationToken cancellationToken) =>
        statistics.GetStatisticsAsync(query.UserId, query.DateFrom, query.DateTo, query.QuantizationDays, cancellationToken);
}
