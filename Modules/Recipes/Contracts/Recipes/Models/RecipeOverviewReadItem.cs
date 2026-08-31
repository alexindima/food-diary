using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Recipes.Models;

public sealed record RecipeOverviewReadItem(
    RecipeId Id,
    UserId UserId,
    string Name,
    string? Description,
    string? Comment,
    string? Category,
    string? ImageUrl,
    ImageAssetId? ImageAssetId,
    int? PrepTime,
    int? CookTime,
    int Servings,
    double? TotalCalories,
    double? TotalProteins,
    double? TotalFats,
    double? TotalCarbs,
    double? TotalFiber,
    double? TotalAlcohol,
    bool IsNutritionAutoCalculated,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    double? ManualAlcohol,
    Visibility Visibility,
    int UsageCount,
    DateTime CreatedOnUtc,
    bool IsOwnedByCurrentUser,
    int QualityScore,
    string QualityGrade,
    IReadOnlyList<RecipeOverviewStepReadItem> Steps);
