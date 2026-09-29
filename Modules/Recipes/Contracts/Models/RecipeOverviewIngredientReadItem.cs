namespace FoodDiary.Modules.Recipes.Contracts.Models;

public sealed record RecipeOverviewIngredientReadItem(
    Guid Id,
    double Amount,
    Guid? ProductId,
    string? ProductName,
    string? ProductBaseUnit,
    double? ProductBaseAmount,
    double? ProductCaloriesPerBase,
    double? ProductProteinsPerBase,
    double? ProductFatsPerBase,
    double? ProductCarbsPerBase,
    double? ProductFiberPerBase,
    double? ProductAlcoholPerBase,
    Guid? NestedRecipeId,
    string? NestedRecipeName,
    int? NestedRecipeServings,
    double? NestedRecipeTotalCalories,
    double? NestedRecipeTotalProteins,
    double? NestedRecipeTotalFats,
    double? NestedRecipeTotalCarbs,
    double? NestedRecipeTotalFiber,
    double? NestedRecipeTotalAlcohol,
    bool ProductIsAccessible = true,
    bool NestedRecipeIsAccessible = true) {
    public string? PublicName { get; init; }
    public string? PublicUnit { get; init; }
    public string? TextName { get; init; }
    public string? AmountText { get; init; }
    public int NestedRecipeMissingIngredientCount { get; init; }
}
