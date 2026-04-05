namespace FoodDiary.Application.MealPlans.Models;

public sealed record MealPlanModel(
    Guid Id,
    string Name,
    string? Description,
    string DietType,
    int DurationDays,
    double? TargetCaloriesPerDay,
    bool IsCurated,
    IReadOnlyList<MealPlanDayModel> Days);

public sealed record MealPlanDayModel(
    Guid Id,
    int DayNumber,
    IReadOnlyList<MealPlanMealModel> Meals);

public sealed record MealPlanMealModel(
    Guid Id,
    string MealType,
    Guid RecipeId,
    string? RecipeName,
    int Servings,
    double? Calories,
    double? Proteins,
    double? Fats,
    double? Carbs);

public sealed record MealPlanSummaryModel(
    Guid Id,
    string Name,
    string? Description,
    string DietType,
    int DurationDays,
    double? TargetCaloriesPerDay,
    bool IsCurated,
    int TotalRecipes);
