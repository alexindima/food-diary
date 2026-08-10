using FoodDiary.Application.Abstractions.WeeklyGoals.Common;
using FoodDiary.Application.WeeklyGoals.Common;
using FoodDiary.Application.WeeklyGoals.Models;
using FoodDiary.Domain.Entities.WeeklyGoals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeeklyGoals.Services;

public sealed class WeeklyGoalReadService(
    IWeeklyGoalRepository goalRepository,
    WeeklyGoalProgressReader progressReader)
    : IWeeklyGoalReadService {
    public async Task<WeeklyGoalModel?> GetAsync(
        UserId userId,
        DateTime weekStartUtc,
        CancellationToken cancellationToken) {
        WeeklyGoal? goal = await goalRepository
            .GetAsync(userId, weekStartUtc, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (goal is null) {
            return null;
        }

        int progressDays = await progressReader.GetProgressDaysAsync(goal, cancellationToken).ConfigureAwait(false);
        return goal.ToModel(progressDays);
    }
}
