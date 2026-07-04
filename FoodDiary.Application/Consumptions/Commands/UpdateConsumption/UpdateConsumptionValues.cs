using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Commands.UpdateConsumption;

internal sealed record UpdateConsumptionValues(
    UserId UserId,
    MealId MealId,
    Meal Meal,
    MealType? MealType,
    ImageAssetId? ImageAssetId,
    ImageAsset? ImageAsset,
    ImageAssetId? OldAssetId);
