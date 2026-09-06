using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dashboard.Infrastructure.Persistence.Dashboard;

internal sealed class DashboardStatisticsReadService(IMealNutritionStatisticsReadService meals) : IDashboardStatisticsReadService {
    public async Task<Result<IReadOnlyList<DashboardStatisticsBucketReadModel>>> GetStatisticsAsync(
        UserId userId, DateTime dateFrom, DateTime dateTo, int quantizationDays, CancellationToken cancellationToken = default) {
        Result<IReadOnlyList<MealNutritionStatisticsBucket>> result = await meals.GetStatisticsAsync(userId, dateFrom, dateTo, quantizationDays, cancellationToken).ConfigureAwait(false);
        return result.IsFailure
            ? Result.Failure<IReadOnlyList<DashboardStatisticsBucketReadModel>>(result.Error)
            : Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([.. result.Value.Select(bucket => new DashboardStatisticsBucketReadModel(
                bucket.DateFrom, bucket.DateTo, bucket.TotalCalories, bucket.AverageProteins, bucket.AverageFats, bucket.AverageCarbs, bucket.AverageFiber,
                bucket.TotalProteins, bucket.TotalFats, bucket.TotalCarbs, bucket.TotalFiber, bucket.BreakfastCalories, bucket.LunchCalories, bucket.DinnerCalories,
                bucket.SnackCalories, bucket.MealCount, bucket.TrackedDayCount))]);
    }
}
