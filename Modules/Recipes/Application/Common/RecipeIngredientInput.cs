namespace FoodDiary.Modules.Recipes.Application.Common;

public record RecipeIngredientInput(
    Guid? ProductId,
    Guid? NestedRecipeId,
    double Amount);
