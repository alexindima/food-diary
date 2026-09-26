namespace FoodDiary.Modules.Recipes.Application.Common;

public record RecipeIngredientInput(
    Guid? ProductId,
    Guid? NestedRecipeId,
    double Amount) {
    public string? TextName { get; init; }
    public string? AmountText { get; init; }
}
