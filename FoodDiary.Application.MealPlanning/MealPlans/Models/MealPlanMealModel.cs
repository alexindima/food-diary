namespace FoodDiary.Application.MealPlans.Models;

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
