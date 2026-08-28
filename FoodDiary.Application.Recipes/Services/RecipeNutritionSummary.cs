namespace FoodDiary.Application.Recipes.Services;

public sealed record RecipeNutritionSummary(
    double? TotalCalories,
    double? TotalProteins,
    double? TotalFats,
    double? TotalCarbs,
    double? TotalFiber,
    double? TotalAlcohol);
