namespace FoodDiary.Application.Abstractions.MealPlans.Models;

public sealed record MealPlanSummaryReadModel(
    Guid Id,
    string Name,
    string? Description,
    string DietType,
    int DurationDays,
    double? TargetCaloriesPerDay,
    bool IsCurated,
    int TotalRecipes);
