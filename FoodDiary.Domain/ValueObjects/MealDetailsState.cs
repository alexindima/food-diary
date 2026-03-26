using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct MealDetailsState(
    DateTime Date,
    MealType? MealType,
    string? Comment,
    string? ImageUrl,
    ImageAssetId? ImageAssetId,
    int PreMealSatietyLevel,
    int PostMealSatietyLevel);
