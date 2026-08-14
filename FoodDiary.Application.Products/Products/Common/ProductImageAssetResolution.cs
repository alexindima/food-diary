using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Products.Common;

internal sealed record ProductImageAssetResolution(
    ImageAssetId? ImageAssetId,
    string? ImageUrl,
    bool HasResolvedImageAsset);
