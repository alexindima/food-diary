using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler(IProductRepository productRepository)
    : ICommandHandler<CreateProductCommand, Result<ProductModel>> {
    public async Task<Result<ProductModel>>
        Handle(CreateProductCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ProductModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);

        if (!Enum.TryParse<MeasurementUnit>(command.BaseUnit, true, out var baseUnit)) {
            return Result.Failure<ProductModel>(
                Errors.Validation.Invalid(nameof(command.BaseUnit), "Unknown measurement unit value."));
        }

        if (!Enum.TryParse<Visibility>(command.Visibility, true, out var visibility)) {
            return Result.Failure<ProductModel>(
                Errors.Validation.Invalid(nameof(command.Visibility), "Unknown visibility value."));
        }

        var productType = Enum.TryParse<ProductType>(command.ProductType, true, out var parsedType)
            ? parsedType
            : ProductType.Unknown;

        var product = Product.Create(
            userId: userId,
            name: command.Name,
            baseUnit: baseUnit,
            baseAmount: command.BaseAmount,
            defaultPortionAmount: command.DefaultPortionAmount,
            caloriesPerBase: command.CaloriesPerBase,
            proteinsPerBase: command.ProteinsPerBase,
            fatsPerBase: command.FatsPerBase,
            carbsPerBase: command.CarbsPerBase,
            fiberPerBase: command.FiberPerBase,
            alcoholPerBase: command.AlcoholPerBase,
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

        product = await productRepository.AddAsync(product, cancellationToken);

        return Result.Success(product.ToModel(isOwnedByCurrentUser: true));
    }
}
