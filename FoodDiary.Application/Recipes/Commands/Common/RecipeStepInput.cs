using System;
using System.Collections.Generic;

namespace FoodDiary.Application.Recipes.Commands.Common;

public record RecipeStepInput(
    int Order,
    string Description,
    string? Title,
    string? ImageUrl,
    Guid? ImageAssetId,
    IReadOnlyList<RecipeIngredientInput> Ingredients);
