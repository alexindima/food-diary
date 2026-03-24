namespace FoodDiary.Presentation.Api.Features.Goals.Requests;

public sealed record UpdateGoalsHttpRequest(
    double? DailyCalorieTarget,
    double? ProteinTarget,
    double? FatTarget,
    double? CarbTarget,
    double? FiberTarget,
    double? WaterGoal,
    double? DesiredWeight,
    double? DesiredWaist
);
