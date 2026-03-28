namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserGoalUpdate(
    double? DailyCalorieTarget = null,
    double? ProteinTarget = null,
    double? FatTarget = null,
    double? CarbTarget = null,
    double? FiberTarget = null,
    double? WaterGoal = null,
    double? DesiredWeight = null,
    double? DesiredWaist = null);
