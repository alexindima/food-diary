using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler(
    IProductRepository productRepository,
    IUserRepository userRepository,
    IImageAssetAccessService imageAssetAccessService)
    : ICommandHandler<CreateProductCommand, Result<ProductModel>> {
    public async Task<Result<ProductModel>>
        Handle(CreateProductCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ProductModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<ProductModel>(accessError);
        }

        var baseUnitResult = EnumValueParser.ParseRequired<MeasurementUnit>(
            command.BaseUnit,
            nameof(command.BaseUnit),
            "Unknown measurement unit value.");
        if (baseUnitResult.IsFailure) {
            return Result.Failure<ProductModel>(baseUnitResult.Error);
        }

        var visibilityResult = EnumValueParser.ParseRequired<Visibility>(
            command.Visibility,
            nameof(command.Visibility),
            "Unknown visibility value.");
        if (visibilityResult.IsFailure) {
            return Result.Failure<ProductModel>(visibilityResult.Error);
        }

        var productTypeResult = EnumValueParser.ParseRequired<ProductType>(
            command.ProductType,
            nameof(command.ProductType),
            "Unknown product type value.");
        if (productTypeResult.IsFailure) {
            return Result.Failure<ProductModel>(productTypeResult.Error);
        }

        if (!Enum.IsDefined(productTypeResult.Value)) {
            return Result.Failure<ProductModel>(Errors.Validation.Invalid(nameof(command.ProductType), "Unknown product type value."));
        }

        var imageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<ProductModel>(imageAssetIdResult.Error);
        }

        var imageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            imageAssetIdResult.Value,
            userId,
            cancellationToken);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<ProductModel>(imageAssetResult.Error);
        }

        var imageUrl = imageAssetResult.Value?.Url ?? command.ImageUrl;

        var product = Product.Create(
            userId: userId,
            name: command.Name,
            baseUnit: baseUnitResult.Value,
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
            productType: productTypeResult.Value,
            category: command.Category,
            description: command.Description,
            comment: command.Comment,
            imageUrl: imageUrl,
            imageAssetId: imageAssetIdResult.Value,
            visibility: visibilityResult.Value
        );

        product = await productRepository.AddAsync(product, cancellationToken);

        return Result.Success(product.ToModel(isOwnedByCurrentUser: true));
    }
}
