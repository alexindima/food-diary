using System;

namespace FoodDiary.Contracts.Recipes;

public record RecipeIngredientRequest(
    Guid? ProductId,
    Guid? NestedRecipeId,
    double Amount);
