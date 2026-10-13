namespace FoodDiary.Modules.Ai.Presentation.Responses;

public sealed record RecipeImportNutritionHttpResponse(
    string Basis, double? Calories, double? Proteins, double? Fats, double? Carbs,
    double? Fiber, double? Alcohol);
