namespace FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

public sealed record UserDailyGoals(
    UserCalorieSchedule CalorieSchedule,
    double? ProteinTarget,
    double? FatTarget,
    double? CarbTarget,
    double? FiberTarget,
    double? WaterGoal,
    double? HydrationGoal,
    int? StepGoal) {
    public double? EffectiveWaterGoal => HydrationGoal ?? WaterGoal;
}
