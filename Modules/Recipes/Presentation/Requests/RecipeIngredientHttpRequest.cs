namespace FoodDiary.Modules.Recipes.Presentation.Requests;

public sealed record RecipeIngredientHttpRequest(
    Guid? ProductId,
    Guid? NestedRecipeId,
    double Amount);
