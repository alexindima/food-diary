using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Users.Domain.ValueObjects;

public readonly record struct UserGoalUpdate(
    double? DailyCalorieTarget = null,
    double? ProteinTarget = null,
    double? FatTarget = null,
    double? CarbTarget = null,
    double? FiberTarget = null,
    double? WaterGoal = null,
    DesiredWeightKg? DesiredWeightKg = null,
    DesiredWaistCm? DesiredWaistCm = null,
    bool? CalorieCyclingEnabled = null,
    double? MondayCalories = null,
    double? TuesdayCalories = null,
    double? WednesdayCalories = null,
    double? ThursdayCalories = null,
    double? FridayCalories = null,
    double? SaturdayCalories = null,
    double? SundayCalories = null);
