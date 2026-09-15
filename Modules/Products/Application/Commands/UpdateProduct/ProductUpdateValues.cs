using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Commands.UpdateProduct;

internal sealed record ProductUpdateValues(
    UserId UserId,
    ProductId ProductId,
    MeasurementUnit? Unit,
    Visibility? Visibility,
    ProductType? ProductType,
    ImageAssetId? ImageAssetId,
    string? ImageUrl,
    bool HasResolvedImageAsset);
