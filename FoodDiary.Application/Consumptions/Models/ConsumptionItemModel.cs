using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Application.Consumptions.Models;

[ExcludeFromCodeCoverage]
public sealed record ConsumptionItemModel(
    Guid Id,
    Guid ConsumptionId,
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
    int? ProductQualityScore,
    string? ProductQualityGrade);
