using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler(IProductRepository productRepository)
    : ICommandHandler<CreateProductCommand, Result<ProductResponse>> {
    public async Task<Result<ProductResponse>>
        Handle(CreateProductCommand command, CancellationToken cancellationToken) {
        var baseUnit = Enum.Parse<MeasurementUnit>(command.BaseUnit, true);
        var visibility = Enum.Parse<Visibility>(command.Visibility, true);
        var productType = Enum.TryParse<ProductType>(command.ProductType, true, out var parsedType)
            ? parsedType
            : ProductType.Unknown;

        var product = Product.Create(
            userId: command.UserId!.Value,
            name: command.Name,
            baseUnit: baseUnit,
            baseAmount: command.BaseAmount,
            defaultPortionAmount: command.DefaultPortionAmount,
            caloriesPerBase: command.CaloriesPerBase,
            proteinsPerBase: command.ProteinsPerBase,
            fatsPerBase: command.FatsPerBase,
            carbsPerBase: command.CarbsPerBase,
            fiberPerBase: command.FiberPerBase,
            barcode: command.Barcode,
            brand: command.Brand,
            productType: productType,
            category: command.Category,
            description: command.Description,
            comment: command.Comment,
            imageUrl: command.ImageUrl,
            imageAssetId: command.ImageAssetId.HasValue ? new ImageAssetId(command.ImageAssetId.Value) : null,
            visibility: visibility
        );

        product = await productRepository.AddAsync(product);

        return Result.Success(product.ToResponse(isOwnedByCurrentUser: true));
    }
}
