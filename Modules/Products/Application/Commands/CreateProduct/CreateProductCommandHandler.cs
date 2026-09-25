using FoodDiary.Modules.Products.Application.Mappings;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Products.Application.Abstractions.Common;

using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Products.Domain.Entities;

namespace FoodDiary.Modules.Products.Application.Commands.CreateProduct;

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
        if (valuesResult.Value.Images is { } images) { product.ReplaceImages(images); }
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
