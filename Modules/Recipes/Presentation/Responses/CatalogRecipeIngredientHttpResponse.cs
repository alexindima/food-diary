namespace FoodDiary.Modules.Recipes.Presentation.Responses;

public sealed record CatalogRecipeIngredientHttpResponse(
    Guid? ProductId,
    Guid? NestedRecipeId,
    double Amount) {
    public string? PublicName { get; init; }
    public string? PublicUnit { get; init; }
    public string? TextName { get; init; }
    public string? AmountText { get; init; }
}
