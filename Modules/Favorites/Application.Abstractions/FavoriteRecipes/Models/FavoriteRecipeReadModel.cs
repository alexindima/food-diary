namespace FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Models;

public sealed record FavoriteRecipeReadModel(
    Guid Id,
    Guid RecipeId,
    string? Name,
    DateTime CreatedAtUtc,
    string RecipeName,
    string? ImageUrl,
    double? TotalCalories,
    int Servings,
    int? PrepTime,
    int? CookTime,
    int IngredientCount) {
    public double TotalProteins { get; init; }
    public double TotalFats { get; init; }
    public double TotalCarbs { get; init; }
    public double TotalFiber { get; init; }
    public IReadOnlyList<string> IngredientNames { get; init; } = [];
}
