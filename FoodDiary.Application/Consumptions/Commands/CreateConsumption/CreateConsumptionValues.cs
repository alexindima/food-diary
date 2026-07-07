using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Commands.CreateConsumption;

internal sealed record CreateConsumptionValues(
    UserId UserId,
    MealType? MealType,
    ImageAssetId? ImageAssetId,
    ImageAsset? ImageAsset);
