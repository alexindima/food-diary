using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler(IProductRepository productRepository)
    : ICommandHandler<UpdateProductCommand, Result<ProductResponse>> {
    public async Task<Result<ProductResponse>>
        Handle(UpdateProductCommand command, CancellationToken cancellationToken) {
        var product = await productRepository.GetByIdAsync(
            command.ProductId,
            command.UserId!.Value,
            includePublic: false,
            cancellationToken: cancellationToken);
        if (product is null) {
            return Result.Failure<ProductResponse>(Errors.Product.NotAccessible(command.ProductId.Value));
        }

        MeasurementUnit? newUnit = null;
        if (!string.IsNullOrWhiteSpace(command.BaseUnit)) {
            newUnit = Enum.Parse<MeasurementUnit>(command.BaseUnit, true);
        }

        Visibility? newVisibility = null;
        if (!string.IsNullOrWhiteSpace(command.Visibility)) {
            newVisibility = Enum.Parse<Visibility>(command.Visibility, true);
        }

        ProductType? newProductType = null;
        if (!string.IsNullOrWhiteSpace(command.ProductType) &&
            Enum.TryParse<ProductType>(command.ProductType, true, out var parsedProductType))
        {
            newProductType = parsedProductType;
        }

        product.Update(
            name: command.Name,
            baseUnit: newUnit,
            baseAmount: command.BaseAmount,
            caloriesPerBase: command.CaloriesPerBase,
            proteinsPerBase: command.ProteinsPerBase,
            fatsPerBase: command.FatsPerBase,
            carbsPerBase: command.CarbsPerBase,
            fiberPerBase: command.FiberPerBase,
            barcode: command.Barcode,
            brand: command.Brand,
            productType: newProductType,
            category: command.Category,
            description: command.Description,
            comment: command.Comment,
            imageUrl: command.ImageUrl,
            visibility: newVisibility);

        await productRepository.UpdateAsync(product);

        var usageCount = product.MealItems.Count + product.RecipeIngredients.Count;
        return Result.Success(product.ToResponse(usageCount, true));
    }
}
