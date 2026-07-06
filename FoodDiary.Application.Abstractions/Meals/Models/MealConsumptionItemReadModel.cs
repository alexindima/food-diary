using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Meals.Models;

public sealed record MealConsumptionItemReadModel(
    Guid Id,
    Guid MealId,
    double Amount,
    Guid? ProductId,
    string? ProductName,
    string? ProductImageUrl,
    string? ProductBaseUnit,
    double? ProductBaseAmount,
    double? ProductCaloriesPerBase,
    double? ProductProteinsPerBase,
    double? ProductFatsPerBase,
    double? ProductCarbsPerBase,
    double? ProductFiberPerBase,
    double? ProductAlcoholPerBase,
    ProductType? ProductType,
    Guid? RecipeId,
    string? RecipeName,
    string? RecipeImageUrl,
    int? RecipeServings,
    double? RecipeTotalCalories,
    double? RecipeTotalProteins,
    double? RecipeTotalFats,
    double? RecipeTotalCarbs,
    double? RecipeTotalFiber,
    double? RecipeTotalAlcohol,
    Guid? SourceAiItemId,
    MealItemOrigin Origin);
