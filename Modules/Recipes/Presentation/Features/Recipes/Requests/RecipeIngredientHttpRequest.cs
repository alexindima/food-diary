namespace FoodDiary.Presentation.Api.Features.Recipes.Requests;

public sealed record RecipeIngredientHttpRequest(
    Guid? ProductId,
    Guid? NestedRecipeId,
    double Amount);
