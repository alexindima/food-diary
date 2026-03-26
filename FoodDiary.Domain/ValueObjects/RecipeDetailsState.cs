using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct RecipeDetailsState(
    string Name,
    string? Description,
    string? Comment,
    string? Category,
    string? ImageUrl,
    ImageAssetId? ImageAssetId,
    int? PrepTime,
    int? CookTime,
    int Servings,
    Visibility Visibility);
