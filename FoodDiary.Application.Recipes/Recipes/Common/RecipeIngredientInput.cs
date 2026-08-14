namespace FoodDiary.Application.Recipes.Recipes.Common;

public record RecipeIngredientInput(
    Guid? ProductId,
    Guid? NestedRecipeId,
    double Amount);
