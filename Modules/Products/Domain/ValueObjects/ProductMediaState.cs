using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct ProductMediaState(
    string? ImageUrl,
    ImageAssetId? ImageAssetId);
