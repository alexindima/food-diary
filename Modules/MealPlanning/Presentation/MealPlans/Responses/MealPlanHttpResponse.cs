namespace FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Responses;

public sealed record MealPlanHttpResponse(
    Guid Id,
    string Name,
    string? Description,
    string DietType,
    int DurationDays,
    double? TargetCaloriesPerDay,
    bool IsCurated,
    IReadOnlyList<MealPlanDayHttpResponse> Days);
