using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

internal sealed record ProductUpdateValues(
    UserId UserId,
    ProductId ProductId,
    MeasurementUnit? Unit,
    Visibility? Visibility,
    ProductType? ProductType,
    ImageAssetId? ImageAssetId,
    string? ImageUrl,
    bool HasResolvedImageAsset);
