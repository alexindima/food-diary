namespace FoodDiary.Modules.Dietologist.Presentation.Responses;

public sealed record ClientGoalsHttpResponse(
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
