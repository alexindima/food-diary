using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserGamificationProfileModel(UserCalorieSchedule CalorieSchedule, UserGoalTimeline? GoalHistory = null) {
    public double? GetCalorieTargetForDate(DateTime date) =>
        (GoalHistory?.GetGoalsForDate(DateOnly.FromDateTime(date))?.CalorieSchedule ?? CalorieSchedule).GetTargetForDate(date);
}
