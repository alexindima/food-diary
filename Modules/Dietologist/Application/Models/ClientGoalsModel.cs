namespace FoodDiary.Modules.Dietologist.Application.Models;

public sealed record ClientGoalsModel(
    Guid Id,
    double? DailyCalorieTarget,
    double? ProteinTarget,
    double? FatTarget,
    double? CarbTarget,
    double? FiberTarget,
    double? WaterGoal,
    double? HydrationGoal,
    double? DesiredWeightKg,
    double? DesiredWaistCm,
    int? StepGoal);
