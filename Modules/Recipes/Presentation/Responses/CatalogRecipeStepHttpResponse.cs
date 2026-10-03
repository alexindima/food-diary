namespace FoodDiary.Modules.Recipes.Presentation.Responses;

public sealed record CatalogRecipeStepHttpResponse(
    int Order,
    string Description,
    string? Title,
    string? ImageUrl,
    Guid? ImageAssetId,
    IReadOnlyList<CatalogRecipeIngredientHttpResponse> Ingredients) {
    public IReadOnlyList<Guid>? ImageAssetIds { get; init; }
}
