using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Commands.CreateProduct;

internal sealed record CreateProductValues(
    UserId UserId,
    MeasurementUnit BaseUnit,
    Visibility Visibility,
    ProductType ProductType,
    ImageAssetId? ImageAssetId,
    string? ImageUrl);
