namespace FoodDiary.Modules.Recipes.Domain.Nutrition;

public sealed record RecipeNutritionValues(
    double? TotalCalories,
    double? TotalProteins,
    double? TotalFats,
    double? TotalCarbs,
    double? TotalFiber,
    double? TotalAlcohol);
