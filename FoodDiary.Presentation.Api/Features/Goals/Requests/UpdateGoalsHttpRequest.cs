namespace FoodDiary.Presentation.Api.Features.Goals.Requests;

public sealed record UpdateGoalsHttpRequest(
    double? DailyCalorieTarget,
    double? ProteinTarget,
    double? FatTarget,
    double? CarbTarget,
    double? FiberTarget,
    double? WaterGoal,
    double? DesiredWeight,
    double? DesiredWaist,
    bool? CalorieCyclingEnabled = null,
    double? MondayCalories = null,
    double? TuesdayCalories = null,
    double? WednesdayCalories = null,
    double? ThursdayCalories = null,
    double? FridayCalories = null,
    double? SaturdayCalories = null,
    double? SundayCalories = null
);
