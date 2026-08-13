using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.CreateProduct;

internal sealed record CreateProductValues(
    UserId UserId,
    MeasurementUnit BaseUnit,
    Visibility Visibility,
    ProductType ProductType,
    ImageAssetId? ImageAssetId,
    string? ImageUrl);
