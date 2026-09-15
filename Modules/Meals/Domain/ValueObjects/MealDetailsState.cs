using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Domain.ValueObjects;

public readonly record struct MealDetailsState(
    DateTime Date,
    MealType? MealType,
    string? Comment,
    string? ImageUrl,
    ImageAssetId? ImageAssetId,
    int PreMealSatietyLevel,
    int PostMealSatietyLevel);
