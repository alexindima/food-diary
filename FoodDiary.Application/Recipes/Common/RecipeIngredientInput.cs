using System;

namespace FoodDiary.Application.Recipes.Common;

public record RecipeIngredientInput(
    Guid? ProductId,
    Guid? NestedRecipeId,
    double Amount);
