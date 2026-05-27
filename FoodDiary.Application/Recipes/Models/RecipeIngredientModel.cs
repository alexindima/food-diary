namespace FoodDiary.Application.Recipes.Models;

public sealed record RecipeIngredientModel(
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
    double? NestedRecipeTotalAlcohol);
