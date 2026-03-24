using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(
    Guid? UserId,
    string? Barcode,
    string Name,
    string? Brand,
    string ProductType,
    string? Category,
    string? Description,
    string? Comment,
    string? ImageUrl,
    Guid? ImageAssetId,
    string BaseUnit,
    double BaseAmount,
    double DefaultPortionAmount,
    double CaloriesPerBase,
    double ProteinsPerBase,
    double FatsPerBase,
    double CarbsPerBase,
    double FiberPerBase,
    double AlcoholPerBase,
    string Visibility
) : ICommand<Result<ProductModel>>;
