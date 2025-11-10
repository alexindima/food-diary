using System.Collections.Generic;

namespace FoodDiary.Contracts.Recipes;

public record RecipeStepRequest(
    string Description,
    IReadOnlyList<RecipeIngredientRequest> Ingredients,
    string? ImageUrl);
