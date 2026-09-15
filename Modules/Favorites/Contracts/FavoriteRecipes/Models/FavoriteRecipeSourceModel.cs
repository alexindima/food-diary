namespace FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;

public sealed record FavoriteRecipeSourceModel(
    string Name,
    string? ImageUrl,
    double? TotalCalories,
    double? ManualCalories,
    int Servings,
    int? PrepTime,
    int? CookTime,
    int IngredientCount);
