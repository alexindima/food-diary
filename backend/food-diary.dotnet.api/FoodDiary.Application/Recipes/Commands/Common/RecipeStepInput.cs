using System.Collections.Generic;

namespace FoodDiary.Application.Recipes.Commands.Common;

public record RecipeStepInput(
    int Order,
    string Description,
    string? ImageUrl,
    IReadOnlyList<RecipeIngredientInput> Ingredients);
