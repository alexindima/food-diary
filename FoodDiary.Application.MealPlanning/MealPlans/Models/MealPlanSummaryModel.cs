namespace FoodDiary.Application.MealPlanning.MealPlans.Models;

public sealed record MealPlanSummaryModel(
    Guid Id,
    string Name,
    string? Description,
    string DietType,
    int DurationDays,
    double? TargetCaloriesPerDay,
    bool IsCurated,
    int TotalRecipes);
