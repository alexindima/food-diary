using System.Collections.Generic;

namespace FoodDiary.Contracts.Recipes;

public record CreateRecipeRequest(
    string Name,
    string? Description,
    string? Category,
    string? ImageUrl,
    int? PrepTime,
    int? CookTime,
    int Servings,
    string Visibility,
    IReadOnlyList<RecipeStepRequest> Steps);
