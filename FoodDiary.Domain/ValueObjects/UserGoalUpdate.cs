namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserGoalUpdate(
    double? DailyCalorieTarget = null,
    double? ProteinTarget = null,
    double? FatTarget = null,
    double? CarbTarget = null,
    double? FiberTarget = null,
    double? WaterGoal = null,
    double? DesiredWeight = null,
    double? DesiredWaist = null,
    bool? CalorieCyclingEnabled = null,
    double? MondayCalories = null,
    double? TuesdayCalories = null,
    double? WednesdayCalories = null,
    double? ThursdayCalories = null,
    double? FridayCalories = null,
    double? SaturdayCalories = null,
    double? SundayCalories = null);
