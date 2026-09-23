namespace FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;

public sealed record FavoriteRecipeModel(
    Guid Id,
    Guid RecipeId,
    string? Name,
    DateTime CreatedAtUtc,
    string RecipeName,
    string? ImageUrl,
    double? TotalCalories,
    int Servings,
    int? TotalTimeMinutes,
    int IngredientCount) {
    public double TotalProteins { get; init; }
    public double TotalFats { get; init; }
    public double TotalCarbs { get; init; }
    public double TotalFiber { get; init; }
    public IReadOnlyList<string> IngredientNames { get; init; } = [];
}
