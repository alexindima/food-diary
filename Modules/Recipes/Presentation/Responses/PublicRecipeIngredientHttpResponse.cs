namespace FoodDiary.Modules.Recipes.Presentation.Responses;

public sealed record PublicRecipeIngredientHttpResponse(string? Name, double? Amount, string? Unit,
    string? AmountText, Guid? RecipeId, bool IsAvailable) {
    public Guid? ProductId { get; init; }
}
