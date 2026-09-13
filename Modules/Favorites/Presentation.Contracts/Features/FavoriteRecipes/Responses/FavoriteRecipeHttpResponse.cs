namespace FoodDiary.Presentation.Api.Features.FavoriteRecipes.Responses;

public sealed record FavoriteRecipeHttpResponse(
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
