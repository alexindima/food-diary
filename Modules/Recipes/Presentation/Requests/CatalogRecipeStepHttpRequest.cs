namespace FoodDiary.Modules.Recipes.Presentation.Requests;

public sealed record CatalogRecipeStepHttpRequest(
    int Order,
    string Description,
    string? Title,
    string? ImageUrl,
    Guid? ImageAssetId,
    IReadOnlyList<CatalogRecipeIngredientHttpRequest> Ingredients) {
    public IReadOnlyList<Guid>? ImageAssetIds { get; init; }
}
