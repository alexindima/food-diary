using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct ProductMediaState(
    string? ImageUrl,
    ImageAssetId? ImageAssetId);
