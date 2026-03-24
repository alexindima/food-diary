using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid? UserId,
    Guid ProductId,
    string? Barcode,
    bool ClearBarcode,
    string? Name,
    string? Brand,
    bool ClearBrand,
    string? ProductType,
    string? Category,
    bool ClearCategory,
    string? Description,
    bool ClearDescription,
    string? Comment,
    bool ClearComment,
    string? ImageUrl,
    bool ClearImageUrl,
    Guid? ImageAssetId,
    bool ClearImageAssetId,
    string? BaseUnit,
    double? BaseAmount,
    double? DefaultPortionAmount,
    double? CaloriesPerBase,
    double? ProteinsPerBase,
    double? FatsPerBase,
    double? CarbsPerBase,
    double? FiberPerBase,
    double? AlcoholPerBase,
    string? Visibility) : ICommand<Result<ProductModel>>;
