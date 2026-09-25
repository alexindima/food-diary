using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Products.Application.Models;

namespace FoodDiary.Modules.Products.Application.Commands.CreateProduct;

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
) : ICommand<Result<ProductModel>>, IUserRequest {
    public IReadOnlyList<Guid>? ImageAssetIds { get; init; }
}
