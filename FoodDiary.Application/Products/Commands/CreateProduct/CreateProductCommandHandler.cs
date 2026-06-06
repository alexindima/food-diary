using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Assets;

namespace FoodDiary.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler(
    IProductRepository productRepository,
    IUserRepository userRepository,
    IImageAssetAccessService imageAssetAccessService)
    : ICommandHandler<CreateProductCommand, Result<ProductModel>> {
    private sealed record CreateProductValues(
        UserId UserId,
        MeasurementUnit BaseUnit,
        Visibility Visibility,
        ProductType ProductType,
        ImageAssetId? ImageAssetId,
        string? ImageUrl);

    public async Task<Result<ProductModel>>
        Handle(CreateProductCommand command, CancellationToken cancellationToken) {
        Result<CreateProductValues> valuesResult = await PrepareCreateValuesAsync(command, cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<ProductModel>(valuesResult.Error);
        }

        Product product = CreateProduct(command, valuesResult.Value);
        product = await productRepository.AddAsync(product, cancellationToken).ConfigureAwait(false);

        return Result.Success(product.ToModel(isOwnedByCurrentUser: true));
    }

    private async Task<Result<CreateProductValues>> PrepareCreateValuesAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<CreateProductValues>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<CreateProductValues>(accessError);
        }

        Result<MeasurementUnit> baseUnitResult = ParseRequiredEnum<MeasurementUnit>(
            command.BaseUnit,
            nameof(command.BaseUnit),
            "Unknown measurement unit value.");
        if (baseUnitResult.IsFailure) {
            return Result.Failure<CreateProductValues>(baseUnitResult.Error);
        }

        Result<Visibility> visibilityResult = ParseRequiredEnum<Visibility>(
            command.Visibility,
            nameof(command.Visibility),
            "Unknown visibility value.");
        if (visibilityResult.IsFailure) {
            return Result.Failure<CreateProductValues>(visibilityResult.Error);
        }

        Result<ProductType> productTypeResult = ParseRequiredEnum<ProductType>(
            command.ProductType,
            nameof(command.ProductType),
            "Unknown product type value.");
        if (productTypeResult.IsFailure) {
            return Result.Failure<CreateProductValues>(productTypeResult.Error);
        }

        if (!Enum.IsDefined(productTypeResult.Value)) {
            return Result.Failure<CreateProductValues>(
                Errors.Validation.Invalid(nameof(command.ProductType), "Unknown product type value."));
        }

        Result<(ImageAssetId? ImageAssetId, string? ImageUrl)> imageAssetResult = await ResolveImageAssetAsync(command, userId, cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<CreateProductValues>(imageAssetResult.Error);
        }

        return Result.Success(new CreateProductValues(
            userId,
            baseUnitResult.Value,
            visibilityResult.Value,
            productTypeResult.Value,
            imageAssetResult.Value.ImageAssetId,
            imageAssetResult.Value.ImageUrl));
    }

    private static Result<TEnum> ParseRequiredEnum<TEnum>(
        string value,
        string propertyName,
        string errorMessage)
        where TEnum : struct, Enum =>
        EnumValueParser.ParseRequired<TEnum>(value, propertyName, errorMessage);

    private async Task<Result<(ImageAssetId? ImageAssetId, string? ImageUrl)>> ResolveImageAssetAsync(
        CreateProductCommand command,
        UserId userId,
        CancellationToken cancellationToken) {
        Result<ImageAssetId?> imageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<(ImageAssetId? ImageAssetId, string? ImageUrl)>(imageAssetIdResult.Error);
        }

        Result<ImageAsset?> imageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            imageAssetIdResult.Value,
            userId,
            cancellationToken).ConfigureAwait(false);
        return imageAssetResult.IsFailure
            ? Result.Failure<(ImageAssetId? ImageAssetId, string? ImageUrl)>(imageAssetResult.Error)
            : Result.Success((imageAssetIdResult.Value, imageAssetResult.Value?.Url ?? command.ImageUrl));
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
