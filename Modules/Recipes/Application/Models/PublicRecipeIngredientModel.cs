using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Modules.Recipes.Application.Models;

[ExcludeFromCodeCoverage]
public sealed record PublicRecipeIngredientModel(string? Name, double? Amount, string? Unit,
    string? AmountText, Guid? RecipeId, bool IsAvailable) {
    public Guid? ProductId { get; init; }
}
