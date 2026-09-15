namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;

public sealed record MealPlanModel(
    Guid Id,
    string Name,
    string? Description,
    string DietType,
    int DurationDays,
    double? TargetCaloriesPerDay,
    bool IsCurated,
    IReadOnlyList<MealPlanDayModel> Days);
