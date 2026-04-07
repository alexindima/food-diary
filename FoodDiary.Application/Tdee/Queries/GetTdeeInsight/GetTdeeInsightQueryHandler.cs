using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Exercises.Common;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Tdee.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WeightEntries.Common;

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
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<TdeeInsightModel>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<TdeeInsightModel>(accessError);
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure<TdeeInsightModel>(Errors.User.NotFound());
        }

        var today = dateTimeProvider.UtcNow.Date;
        var periodStart = today.AddDays(-AnalysisPeriodDays);

        var weightsTask = weightEntryRepository.GetByPeriodAsync(userId, periodStart, today, cancellationToken);
        var mealsTask = mealRepository.GetByPeriodAsync(userId, periodStart, today, cancellationToken);
        var exercisesTask = exerciseEntryRepository.GetByDateRangeAsync(userId, periodStart, today, cancellationToken);
        await Task.WhenAll(weightsTask, mealsTask, exercisesTask);

        var weights = weightsTask.Result;
        var meals = mealsTask.Result;
        var exercises = exercisesTask.Result;

        var bmr = user.CalculateBmr();
        var estimatedTdee = user.CalculateEstimatedTdee();

        var adaptiveResult = TdeeCalculator.CalculateAdaptive(weights, meals, AnalysisPeriodDays, exercises);

        var effectiveTdee = adaptiveResult.HasData ? adaptiveResult.AdaptiveTdee : estimatedTdee;
        var suggestedTarget = effectiveTdee.HasValue
            ? TdeeCalculator.SuggestCalorieTarget(effectiveTdee.Value, user.Weight, user.DesiredWeight)
            : null;

        var hint = TdeeCalculator.GetGoalAdjustmentHint(
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
