namespace FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;

public sealed record FavoriteRecipeModel(
    Guid Id,
    Guid RecipeId,
    string? Name,
    DateTime CreatedAtUtc,
    string RecipeName,
    string? ImageUrl,
    double? TotalCalories,
    int Servings,
    int? TotalTimeMinutes,
    int IngredientCount);
