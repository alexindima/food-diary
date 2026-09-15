using FoodDiary.Modules.Dashboard.Application.Abstractions.Common;
using FoodDiary.Modules.Dashboard.Contracts.Models;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dashboard.Infrastructure.Persistence;

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
