using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Exercises.Common;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Application.Tdee.Common;
using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Tdee.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tdee.Queries.GetTdeeInsight;

public sealed class GetTdeeInsightQueryHandler(
    ITdeeUserProfileService tdeeUserProfileService,
    IWeightEntryReadService weightEntryReadService,
    IDashboardStatisticsReadService statisticsReadService,
    IExerciseEntryReadService exerciseEntryReadService,
    TimeProvider dateTimeProvider,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetTdeeInsightQuery, Result<TdeeInsightModel>> {
    private const int AnalysisPeriodDays = 28;

    public async Task<Result<TdeeInsightModel>> Handle(
        GetTdeeInsightQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<TdeeInsightModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<TdeeUserProfile> profileResult = await tdeeUserProfileService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<TdeeInsightModel>(profileResult.Error);
        }

        TdeeUserProfile profile = profileResult.Value;

        DateTime today = dateTimeProvider.GetUtcNow().UtcDateTime.Date;
        DateTime periodStart = today.AddDays(-AnalysisPeriodDays);

        IReadOnlyList<WeightEntryModel> weights = await weightEntryReadService
            .GetEntriesAsync(userId, periodStart, today, limit: null, descending: false, cancellationToken)
            .ConfigureAwait(false);
        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> dailyCaloriesResult = await statisticsReadService.GetStatisticsAsync(
            userId,
            periodStart,
            today,
            quantizationDays: 1,
            cancellationToken).ConfigureAwait(false);
        if (dailyCaloriesResult.IsFailure) {
            return Result.Failure<TdeeInsightModel>(dailyCaloriesResult.Error);
        }

        IReadOnlyList<ExerciseEntryModel> exercises = await exerciseEntryReadService
            .GetEntriesAsync(userId, periodStart, today, cancellationToken)
            .ConfigureAwait(false);

        double? bmr = profile.Bmr;
        double? estimatedTdee = profile.EstimatedTdee;

        AdaptiveTdeeResult adaptiveResult = CalculateAdaptive(weights, dailyCaloriesResult.Value, exercises);

        double? effectiveTdee = adaptiveResult.HasData ? adaptiveResult.AdaptiveTdee : estimatedTdee;
        double? suggestedTarget = effectiveTdee.HasValue
            ? TdeeCalculator.SuggestCalorieTarget(effectiveTdee.Value, profile.Weight, profile.DesiredWeight)
            : null;

        string? hint = TdeeCalculator.GetGoalAdjustmentHint(
            effectiveTdee, profile.DailyCalorieTarget, profile.Weight, profile.DesiredWeight);

        return Result.Success(new TdeeInsightModel(
            EstimatedTdee: estimatedTdee,
            AdaptiveTdee: adaptiveResult.AdaptiveTdee,
            Bmr: bmr,
            SuggestedCalorieTarget: suggestedTarget,
            CurrentCalorieTarget: profile.DailyCalorieTarget,
            WeightTrendPerWeek: adaptiveResult.WeightTrendPerWeek,
            Confidence: adaptiveResult.HasData ? adaptiveResult.Confidence : TdeeConfidence.None,
            DataDaysUsed: adaptiveResult.DataDaysUsed,
            GoalAdjustmentHint: hint));
    }

    private static IReadOnlyDictionary<DateTime, double> ToDailyCalories(IReadOnlyList<DashboardStatisticsBucketReadModel> buckets) =>
        buckets
            .Where(static bucket => bucket.TotalCalories > 0)
            .ToDictionary(static bucket => bucket.DateFrom.Date, static bucket => bucket.TotalCalories);

    private static AdaptiveTdeeResult CalculateAdaptive(
        IReadOnlyList<WeightEntryModel> weights,
        IReadOnlyList<DashboardStatisticsBucketReadModel> dailyCalories,
        IReadOnlyList<ExerciseEntryModel> exercises) =>
        TdeeCalculator.CalculateAdaptive(weights, ToDailyCalories(dailyCalories), AnalysisPeriodDays, exercises);
}
