namespace FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Responses;

public sealed record MealPlanSummaryHttpResponse(
    Guid Id,
    string Name,
    string? Description,
    string DietType,
    int DurationDays,
    double? TargetCaloriesPerDay,
    bool IsCurated,
    int TotalRecipes);
