using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;

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
            visibility: visibility
        );

        product = await productRepository.AddAsync(product);

        return Result.Success(product.ToResponse(isOwnedByCurrentUser: true));
    }
}
