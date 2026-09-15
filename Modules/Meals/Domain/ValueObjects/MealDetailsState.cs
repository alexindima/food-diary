using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct MealDetailsState(
    DateTime Date,
    MealType? MealType,
    string? Comment,
    string? ImageUrl,
    ImageAssetId? ImageAssetId,
    int PreMealSatietyLevel,
    int PostMealSatietyLevel);
