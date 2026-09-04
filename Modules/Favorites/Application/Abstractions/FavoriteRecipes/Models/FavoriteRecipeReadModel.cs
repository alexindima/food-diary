namespace FoodDiary.Application.Abstractions.FavoriteRecipes.Models;

public sealed record FavoriteRecipeReadModel(
    Guid Id,
    Guid RecipeId,
    string? Name,
    DateTime CreatedAtUtc,
    string RecipeName,
    string? ImageUrl,
    double? TotalCalories,
    int Servings,
    int? PrepTime,
    int? CookTime,
    int IngredientCount);
