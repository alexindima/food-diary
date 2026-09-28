using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Modules.Recipes.Application.Models;

[ExcludeFromCodeCoverage]
public sealed record PublicRecipeModel(
    Guid Id, string Name, string? Description, string? Category,
    string? ImageUrl, IReadOnlyList<string> Images, int? PrepTime, int? CookTime, int Servings,
    double? TotalCalories, double? TotalProteins, double? TotalFats, double? TotalCarbs,
    double? TotalFiber, double? TotalAlcohol, int MissingIngredientCount,
    IReadOnlyList<PublicRecipeStepModel> Steps) {
    public string? AuthorName { get; init; }
    public string Language { get; init; } = "en";
    public IReadOnlyList<string> MissingIngredientNames { get; init; } = [];
}
