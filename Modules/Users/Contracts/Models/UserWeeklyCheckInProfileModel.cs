using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserWeeklyCheckInProfileModel(double? DailyCalorieTarget, UserGoalTimeline? GoalHistory = null) {
    public double? GetAverageCalorieTarget(DateOnly from, DateOnly to) {
        if (GoalHistory is null) {
            return DailyCalorieTarget;
        }
        double?[] targets = [.. Enumerable.Range(0, to.DayNumber - from.DayNumber + 1).Select(offset => {
            DateOnly day = from.AddDays(offset);
            return GoalHistory.GetGoalsForDate(day)?.CalorieSchedule.GetTargetForDate(day.ToDateTime(TimeOnly.MinValue));
        })];
        return targets.Any(target => target is null) ? null : targets.Average();
    }
}
