using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Statistics.Common;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Statistics.Queries.GetDiaryStatistics;

public sealed class GetDiaryStatisticsQueryHandler(ICurrentUserAccessService accessService, IUserDashboardProfileReadService profiles,
    IDashboardStatisticsReadService statistics, IHydrationIntervalReadService hydration, TimeProvider timeProvider)
    : IQueryHandler<GetDiaryStatisticsQuery, Result<DiaryStatisticsSummaryModel>> {
    public async Task<Result<DiaryStatisticsSummaryModel>> Handle(GetDiaryStatisticsQuery query, CancellationToken cancellationToken) {
        Result<UserId> owner = await CurrentUserAccessResolver.ResolveAsync(query.UserId, accessService, cancellationToken).ConfigureAwait(false);
        if (owner.IsFailure) {
            return Result.Failure<DiaryStatisticsSummaryModel>(owner.Error);
        }
        if (query.Days is not (1 or 7)) {
            return Result.Failure<DiaryStatisticsSummaryModel>(new Error("Statistics.InvalidDays", "Choose one or seven calendar days.", ErrorKind.Validation));
        }
        Result<UserDashboardProfileModel> profileResult = await profiles.GetDashboardProfileAsync(owner.Value, cancellationToken).ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<DiaryStatisticsSummaryModel>(profileResult.Error);
        }
        UserDashboardProfileModel profile = profileResult.Value;
        if (profile.Id != owner.Value.Value) {
            return Result.Failure<DiaryStatisticsSummaryModel>(new Error("Statistics.ProfileNotFound", "Profile was not found.", ErrorKind.NotFound));
        }
        string zone = profile.TimeZoneId ?? "UTC";
        IReadOnlyList<LocalStatisticsDay> days;
        try {
            days = LocalStatisticsCalendar.GetDays(timeProvider.GetUtcNow().UtcDateTime, zone, query.Days);
        } catch (TimeZoneNotFoundException) {
            return InvalidTimeZone();
        } catch (InvalidTimeZoneException) {
            return InvalidTimeZone();
        }
        var summaries = new List<DiaryStatisticsDayModel>(days.Count);
        foreach (LocalStatisticsDay day in days) {
            IReadOnlyList<DashboardStatisticsBucketReadModel> buckets = [];
            long waterMl = 0;
            if (day.StartUtc < day.EndExclusiveUtc) {
                // Two-day quantization keeps even a 25-hour local day in one existing owner bucket.
                Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> result = await statistics.GetStatisticsAsync(owner.Value,
                    day.StartUtc, day.EndExclusiveUtc.AddTicks(-10), quantizationDays: 2, cancellationToken).ConfigureAwait(false);
                if (result.IsFailure) {
                    return Result.Failure<DiaryStatisticsSummaryModel>(result.Error);
                }
                buckets = result.Value;
                waterMl = await hydration.GetTotalAsync(owner.Value, day.StartUtc, day.EndExclusiveUtc, cancellationToken).ConfigureAwait(false);
            }
            summaries.Add(new DiaryStatisticsDayModel(day.Date, buckets.Sum(bucket => bucket.TotalCalories), buckets.Sum(bucket => bucket.TotalProteins),
                buckets.Sum(bucket => bucket.TotalFats), buckets.Sum(bucket => bucket.TotalCarbs), buckets.Sum(bucket => bucket.TotalFiber),
                waterMl, buckets.Sum(bucket => bucket.MealCount), profile.GetCalorieTargetForDate(day.Date)));
        }
        return Result.Success(new DiaryStatisticsSummaryModel(zone, summaries, profile.HydrationGoal ?? profile.WaterGoal));
    }

    private static Result<DiaryStatisticsSummaryModel> InvalidTimeZone() => Result.Failure<DiaryStatisticsSummaryModel>(
        new Error("Statistics.InvalidTimeZone", "Update the time zone in your profile.", ErrorKind.Validation));
}
