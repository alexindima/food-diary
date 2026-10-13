namespace FoodDiary.Modules.Ai.Contracts.Models;

public sealed record RecipeImportNutritionModel(
    string Basis, double? Calories, double? Proteins, double? Fats, double? Carbs,
    double? Fiber, double? Alcohol);
