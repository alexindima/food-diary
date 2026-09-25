using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Commands.UpdateProduct;

internal sealed record ProductUpdateValues(
    UserId UserId,
    ProductId ProductId,
    MeasurementUnit? Unit,
    Visibility? Visibility,
    ProductType? ProductType,
    ImageAssetId? ImageAssetId,
    string? ImageUrl,
    bool HasResolvedImageAsset) {
    public IReadOnlyList<FoodDiary.Modules.Products.Domain.Entities.ProductImage>? Images { get; init; }
}
