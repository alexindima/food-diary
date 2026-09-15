using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Common;

internal sealed record ProductImageAssetResolution(
    ImageAssetId? ImageAssetId,
    string? ImageUrl,
    bool HasResolvedImageAsset);
