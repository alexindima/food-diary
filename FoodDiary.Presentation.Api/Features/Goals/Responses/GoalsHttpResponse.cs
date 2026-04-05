namespace FoodDiary.Presentation.Api.Features.Goals.Responses;

public sealed record GoalsHttpResponse(
    double? DailyCalorieTarget,
    double? ProteinTarget,
    double? FatTarget,
    double? CarbTarget,
    double? FiberTarget,
    double? WaterGoal,
    double? DesiredWeight,
    double? DesiredWaist,
    bool CalorieCyclingEnabled,
    double? MondayCalories,
    double? TuesdayCalories,
    double? WednesdayCalories,
    double? ThursdayCalories,
    double? FridayCalories,
    double? SaturdayCalories,
    double? SundayCalories);
