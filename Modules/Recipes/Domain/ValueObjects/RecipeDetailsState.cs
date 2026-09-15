using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;

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
