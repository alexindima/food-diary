using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Tdee.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Tdee.Queries.GetTdeeInsight;

public class GetTdeeInsightQueryHandler(
    IUserRepository userRepository,
    IWeightEntryRepository weightEntryRepository,
    IMealRepository mealRepository,
    IExerciseEntryRepository exerciseEntryRepository,
    IDateTimeProvider dateTimeProvider)
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
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<TdeeInsightModel>(accessError);
        }

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null) {
            return Result.Failure<TdeeInsightModel>(Errors.User.NotFound());
        }

        DateTime today = dateTimeProvider.UtcNow.Date;
        DateTime periodStart = today.AddDays(-AnalysisPeriodDays);

        IReadOnlyList<WeightEntry> weights = await weightEntryRepository.GetByPeriodAsync(userId, periodStart, today, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<Meal> meals = await mealRepository.GetByPeriodAsync(userId, periodStart, today, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<ExerciseEntry> exercises = await exerciseEntryRepository.GetByDateRangeAsync(userId, periodStart, today, cancellationToken).ConfigureAwait(false);

        double? bmr = user.CalculateBmr();
        double? estimatedTdee = user.CalculateEstimatedTdee();

        AdaptiveTdeeResult adaptiveResult = TdeeCalculator.CalculateAdaptive(weights, meals, AnalysisPeriodDays, exercises);

        double? effectiveTdee = adaptiveResult.HasData ? adaptiveResult.AdaptiveTdee : estimatedTdee;
        double? suggestedTarget = effectiveTdee.HasValue
            ? TdeeCalculator.SuggestCalorieTarget(effectiveTdee.Value, user.Weight, user.DesiredWeight)
            : null;

        string? hint = TdeeCalculator.GetGoalAdjustmentHint(
            effectiveTdee, user.DailyCalorieTarget, user.Weight, user.DesiredWeight);

        return Result.Success(new TdeeInsightModel(
            EstimatedTdee: estimatedTdee,
            AdaptiveTdee: adaptiveResult.AdaptiveTdee,
            Bmr: bmr,
            SuggestedCalorieTarget: suggestedTarget,
            CurrentCalorieTarget: user.DailyCalorieTarget,
            WeightTrendPerWeek: adaptiveResult.WeightTrendPerWeek,
            Confidence: adaptiveResult.HasData ? adaptiveResult.Confidence : TdeeConfidence.None,
            DataDaysUsed: adaptiveResult.DataDaysUsed,
            GoalAdjustmentHint: hint));
    }
}
