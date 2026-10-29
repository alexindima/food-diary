using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Dashboard.Application.Models;

public sealed record DashboardUserContextModel(
    Guid Id,
    string? Email,
    string? Language,
    string? DashboardLayoutJson,
    double? DesiredWeightKg,
    double? DesiredWaistCm,
    double? HydrationGoal,
    double? WaterGoal,
    double? ProteinTarget,
    double? FatTarget,
    double? CarbTarget,
    double? FiberTarget,
    UserCalorieSchedule CalorieSchedule,
    string? TimeZoneId = null,
    UserGoalTimeline? GoalHistory = null) {
    public UserDailyGoals GetGoalsForDate(DateOnly date) => GoalHistory?.GetGoalsForDate(date)
        ?? new UserDailyGoals(CalorieSchedule, ProteinTarget, FatTarget, CarbTarget, FiberTarget, WaterGoal, HydrationGoal, StepGoal: null);

    public double? GetCalorieTargetForDate(DateTime date) => GetGoalsForDate(DateOnly.FromDateTime(date)).CalorieSchedule.GetTargetForDate(date);

    public double GetWeeklyCalorieTarget() => CalorieSchedule.GetWeeklyTarget();
}
