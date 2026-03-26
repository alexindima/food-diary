using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct RecipeUpdate(
    string? Name = null,
    string? Description = null,
    string? Comment = null,
    string? Category = null,
    string? ImageUrl = null,
    ImageAssetId? ImageAssetId = null,
    int? PrepTime = null,
    int? CookTime = null,
    int? Servings = null,
    Visibility? Visibility = null);
