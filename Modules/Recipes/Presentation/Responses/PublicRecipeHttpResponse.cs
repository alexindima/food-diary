namespace FoodDiary.Modules.Recipes.Presentation.Responses;

public sealed record PublicRecipeHttpResponse(
    Guid Id, string Name, string? Description, string? Category,
    string? ImageUrl, IReadOnlyList<string> Images, int? PrepTime, int? CookTime, int Servings,
    double? TotalCalories, double? TotalProteins, double? TotalFats, double? TotalCarbs,
    double? TotalFiber, double? TotalAlcohol, int MissingIngredientCount,
    IReadOnlyList<PublicRecipeStepHttpResponse> Steps) {
    public string? AuthorName { get; init; }
    public string Language { get; init; } = "en";
    public IReadOnlyList<string> MissingIngredientNames { get; init; } = [];
}
