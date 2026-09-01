using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct RecipeUpdate(
    string? Name = null,
    string? Description = null,
    bool ClearDescription = false,
    string? Comment = null,
    bool ClearComment = false,
    string? Category = null,
    bool ClearCategory = false,
    string? ImageUrl = null,
    bool ClearImageUrl = false,
    ImageAssetId? ImageAssetId = null,
    bool ClearImageAssetId = false,
    int? PrepTime = null,
    int? CookTime = null,
    int? Servings = null,
    Visibility? Visibility = null);
