namespace FoodDiary.Application.MealPlans.Models;

public sealed record MealPlanSummaryModel(
    Guid Id,
    string Name,
    string? Description,
    string DietType,
    int DurationDays,
    double? TargetCaloriesPerDay,
    bool IsCurated,
    int TotalRecipes);
