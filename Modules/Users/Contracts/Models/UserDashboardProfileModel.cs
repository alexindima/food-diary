using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserDashboardProfileModel(
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
    string? TimeZoneId = null) {
    public double? GetCalorieTargetForDate(DateOnly date) => CalorieSchedule.GetTargetForDate(date.ToDateTime(TimeOnly.MinValue));
}
