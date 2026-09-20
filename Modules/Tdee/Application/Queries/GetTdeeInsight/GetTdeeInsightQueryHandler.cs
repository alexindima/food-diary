using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Tdee.Application.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Tdee.Contracts.Queries.GetTdeeInsight;
using FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseEntries;
using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Exercises.Contracts.Models;
using FoodDiary.Modules.Tdee.Contracts.Models;
using FoodDiary.Modules.Tdee.Application.Services;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;

namespace FoodDiary.Modules.Tdee.Application.Queries.GetTdeeInsight;

public sealed class GetTdeeInsightQueryHandler(
    IUserTdeeProfileReadService userProfileReadService,
    ISender sender,
    IMealDailyCalorieReadService statisticsReadService,
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
        Result<TdeeUserProfile> profileResult = await GetProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<TdeeInsightModel>(profileResult.Error);
        }

        TdeeUserProfile profile = profileResult.Value;

        if (!LocalCalendar.TryResolve(query.TimeZoneId, query.TimeZoneOffsetMinutes, out TimeZoneInfo zone)) {
            return Result.Failure<TdeeInsightModel>(Errors.Validation.Invalid(nameof(query.TimeZoneId), "Unknown time zone."));
        }
        DateOnly calendarDate = query.CurrentDate ?? LocalCalendar.DateAt(dateTimeProvider.GetUtcNow().UtcDateTime, zone);
        DateTime today;
        DateTime periodStart;
        DateTime nutritionStart;
        DateTime nutritionEnd;
        try {
            today = calendarDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            periodStart = today.AddDays(-AnalysisPeriodDays);
            nutritionStart = LocalCalendar.StartOfDayUtc(calendarDate.AddDays(-AnalysisPeriodDays), zone);
            nutritionEnd = LocalCalendar.StartOfDayUtc(calendarDate.AddDays(1), zone).AddTicks(-1);
        } catch (ArgumentOutOfRangeException) {
            return Result.Failure<TdeeInsightModel>(Errors.Validation.Invalid(nameof(query.CurrentDate), "Date range is outside supported boundaries."));
        }

        IReadOnlyList<WeightEntryModel> weights = await sender.Send(new ReadWeightEntriesQuery(UserId: userId, DateFrom: periodStart, DateTo: today, Limit: null, Descending: false), cancellationToken)
            .ConfigureAwait(false);
        Result<IReadOnlyList<MealDailyCalories>> dailyCaloriesResult = await statisticsReadService.GetDailyCaloriesAsync(
            userId,
            nutritionStart,
            nutritionEnd,
            cancellationToken, zone).ConfigureAwait(false);
        if (dailyCaloriesResult.IsFailure) {
            return Result.Failure<TdeeInsightModel>(dailyCaloriesResult.Error);
        }

        IReadOnlyList<ExerciseEntryModel> exercises = await sender.Send(new ReadExerciseEntriesQuery(userId, periodStart, today), cancellationToken)
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

    private static IReadOnlyDictionary<DateTime, double> ToDailyCalories(IReadOnlyList<MealDailyCalories> buckets) =>
        buckets
            .Where(static bucket => bucket.TotalCalories > 0)
            .ToDictionary(static bucket => bucket.Date.Date, static bucket => bucket.TotalCalories);

    private static AdaptiveTdeeResult CalculateAdaptive(
        IReadOnlyList<WeightEntryModel> weights,
        IReadOnlyList<MealDailyCalories> dailyCalories,
        IReadOnlyList<ExerciseEntryModel> exercises) =>
        TdeeCalculator.CalculateAdaptive(weights, ToDailyCalories(dailyCalories), AnalysisPeriodDays, exercises);
    private async Task<Result<TdeeUserProfile>> GetProfileAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<UserTdeeProfileModel> profileResult = await userProfileReadService
            .GetTdeeProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<TdeeUserProfile>(profileResult.Error);
        }

        UserTdeeProfileModel profile = profileResult.Value;
        return Result.Success(new TdeeUserProfile(
            profile.Bmr,
            profile.EstimatedTdee,
            profile.WeightKg,
            profile.DesiredWeightKg,
            profile.DailyCalorieTarget));
    }
}
