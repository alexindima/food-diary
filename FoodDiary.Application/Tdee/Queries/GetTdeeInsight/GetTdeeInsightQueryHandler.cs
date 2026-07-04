using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Tdee.Common;
using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Tdee.Services;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Tdee.Queries.GetTdeeInsight;

public class GetTdeeInsightQueryHandler(
    ITdeeUserProfileService tdeeUserProfileService,
    IWeightEntryReadRepository weightEntryRepository,
    IMealReadRepository mealRepository,
    IExerciseEntryReadRepository exerciseEntryRepository,
    TimeProvider dateTimeProvider)
    : IQueryHandler<GetTdeeInsightQuery, Result<TdeeInsightModel>> {
    private const int AnalysisPeriodDays = 28;

    public async Task<Result<TdeeInsightModel>> Handle(
        GetTdeeInsightQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<TdeeInsightModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Result<TdeeUserProfile> profileResult = await tdeeUserProfileService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<TdeeInsightModel>(profileResult.Error);
        }

        TdeeUserProfile profile = profileResult.Value;

        DateTime today = dateTimeProvider.GetUtcNow().UtcDateTime.Date;
        DateTime periodStart = today.AddDays(-AnalysisPeriodDays);

        IReadOnlyList<WeightEntry> weights = await weightEntryRepository.GetByPeriodAsync(userId, periodStart, today, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<Meal> meals = await mealRepository.GetByPeriodAsync(userId, periodStart, today, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<ExerciseEntry> exercises = await exerciseEntryRepository.GetByDateRangeAsync(userId, periodStart, today, cancellationToken).ConfigureAwait(false);

        double? bmr = profile.Bmr;
        double? estimatedTdee = profile.EstimatedTdee;

        AdaptiveTdeeResult adaptiveResult = TdeeCalculator.CalculateAdaptive(weights, meals, AnalysisPeriodDays, exercises);

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
}
