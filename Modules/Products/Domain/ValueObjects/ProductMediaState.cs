using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Domain.ValueObjects;

public readonly record struct ProductMediaState(
    string? ImageUrl,
    ImageAssetId? ImageAssetId);
