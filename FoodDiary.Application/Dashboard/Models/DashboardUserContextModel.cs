using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Dashboard.Models;

public sealed record DashboardUserContextModel(
    Guid Id,
    string Email,
    string? Language,
    string? DashboardLayoutJson,
    double? DesiredWeight,
    double? DesiredWaist,
    double? HydrationGoal,
    double? WaterGoal,
    double? ProteinTarget,
    double? FatTarget,
    double? CarbTarget,
    double? FiberTarget,
    UserCalorieSchedule CalorieSchedule) {
    public double? GetCalorieTargetForDate(DateTime date) => CalorieSchedule.GetTargetForDate(date);

    public double GetWeeklyCalorieTarget() => CalorieSchedule.GetWeeklyTarget();
}
