using System;

namespace FoodDiary.Application.Recipes.Commands.Common;

public record RecipeIngredientInput(
    Guid? ProductId,
    Guid? NestedRecipeId,
    double Amount);
