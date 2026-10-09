using FoodDiary.Modules.Products.Domain.ValueObjects;
using FoodDiary.Modules.Products.Application.Mappings;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
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
        Product.CreateWithMeasurements(
            userId: values.UserId,
            name: command.Name,
            basis: ProductMeasurementBasis.FromFields(values.BaseUnit, command.BaseAmount),
            defaultPortion: command.DefaultPortionAmount is { } amount ? ProductDefaultPortion.FromAmount(values.BaseUnit, amount) : null,
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
            visibility: values.Visibility,
            importId: command.CatalogImportId is { } id ? new FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids.ProductId(id) : null
        );
}
