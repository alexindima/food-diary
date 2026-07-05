namespace FoodDiary.Application.Abstractions.MealPlans.Models;

public sealed record MealPlanMealReadModel(
    Guid Id,
    string MealType,
    Guid RecipeId,
    string? RecipeName,
    int Servings,
    int RecipeServings,
    double? RecipeTotalCalories,
    double? RecipeTotalProteins,
    double? RecipeTotalFats,
    double? RecipeTotalCarbs);
