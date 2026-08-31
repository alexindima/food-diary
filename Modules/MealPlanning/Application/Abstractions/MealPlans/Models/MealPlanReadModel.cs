namespace FoodDiary.Application.Abstractions.MealPlans.Models;

public sealed record MealPlanReadModel(
    Guid Id,
    Guid? UserId,
    string Name,
    string? Description,
    string DietType,
    int DurationDays,
    double? TargetCaloriesPerDay,
    bool IsCurated,
    IReadOnlyList<MealPlanDayReadModel> Days);
