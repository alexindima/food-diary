using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Products;

namespace FoodDiary.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(
    IProductWriteRepository productRepository,
    ICurrentUserAccessService currentUserAccessService,
    IImageAssetAccessService imageAssetAccessService)
    : ICommandHandler<CreateProductCommand, Result<ProductModel>> {
    public async Task<Result<ProductModel>>
        Handle(CreateProductCommand command, CancellationToken cancellationToken) {
        Result<CreateProductValues> valuesResult = await CreateProductValuePreparer.PrepareAsync(
            command,
            currentUserAccessService,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<ProductModel>(valuesResult.Error);
        }

        Product product = CreateProduct(command, valuesResult.Value);
        product = await productRepository.AddAsync(product, cancellationToken).ConfigureAwait(false);

        return Result.Success(product.ToModel(isOwnedByCurrentUser: true));
    }

    private static Product CreateProduct(
        CreateProductCommand command,
        CreateProductValues values) =>
        Product.Create(
            userId: values.UserId,
            name: command.Name,
            baseUnit: values.BaseUnit,
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
            productType: values.ProductType,
            category: command.Category,
            description: command.Description,
            comment: command.Comment,
            imageUrl: values.ImageUrl,
            imageAssetId: values.ImageAssetId,
            visibility: values.Visibility
        );
}
