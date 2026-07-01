using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

internal static class ProductUpdateValuePreparer {
    public static async Task<Result<ProductUpdateValues>> PrepareAsync(
        UpdateProductCommand command,
        IUserRepository userRepository,
        IImageAssetAccessService imageAssetAccessService,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await ResolveUserIdAsync(command, userRepository, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure<ProductUpdateValues>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        var productId = new ProductId(command.ProductId);
        Result<MeasurementUnit?> unitResult = ParseOptionalEnum<MeasurementUnit>(
            command.BaseUnit,
            nameof(command.BaseUnit),
            "Unknown measurement unit value.");
        if (unitResult.IsFailure) {
            return Result.Failure<ProductUpdateValues>(unitResult.Error);
        }

        Result<Visibility?> visibilityResult = ParseOptionalEnum<Visibility>(
            command.Visibility,
            nameof(command.Visibility),
            "Unknown visibility value.");
        if (visibilityResult.IsFailure) {
            return Result.Failure<ProductUpdateValues>(visibilityResult.Error);
        }

        Result<ProductType?> productTypeResult = ParseProductType(command);
        if (productTypeResult.IsFailure) {
            return Result.Failure<ProductUpdateValues>(productTypeResult.Error);
        }

        Result<ProductUpdateImageValues> imageAssetResult = await ResolveImageAssetAsync(
            command,
            userId,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<ProductUpdateValues>(imageAssetResult.Error);
        }

        return Result.Success(new ProductUpdateValues(
            userId,
            productId,
            unitResult.Value,
            visibilityResult.Value,
            productTypeResult.Value,
            imageAssetResult.Value.ImageAssetId,
            imageAssetResult.Value.ImageUrl,
            imageAssetResult.Value.HasResolvedImageAsset));
    }

    private static async Task<Result<UserId>> ResolveUserIdAsync(
        UpdateProductCommand command,
        IUserRepository userRepository,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<UserId>(Errors.Authentication.InvalidToken);
        }

        if (command.ProductId == Guid.Empty) {
            return Result.Failure<UserId>(
                Errors.Validation.Invalid(nameof(command.ProductId), "Product id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        return accessError is null
            ? Result.Success(userId)
            : Result.Failure<UserId>(accessError);
    }

    private static async Task<Result<ProductUpdateImageValues>> ResolveImageAssetAsync(
        UpdateProductCommand command,
        UserId userId,
        IImageAssetAccessService imageAssetAccessService,
        CancellationToken cancellationToken) {
        Result<ImageAssetId?> imageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<ProductUpdateImageValues>(imageAssetIdResult.Error);
        }

        Result<ImageAsset?> imageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            imageAssetIdResult.Value,
            userId,
            cancellationToken).ConfigureAwait(false);
        return imageAssetResult.IsFailure
            ? Result.Failure<ProductUpdateImageValues>(imageAssetResult.Error)
            : Result.Success(new ProductUpdateImageValues(
                imageAssetIdResult.Value,
                imageAssetResult.Value?.Url ?? command.ImageUrl,
                imageAssetResult.Value is not null));
    }

    private static Result<TEnum?> ParseOptionalEnum<TEnum>(
        string? value,
        string propertyName,
        string errorMessage)
        where TEnum : struct, Enum {
        return string.IsNullOrWhiteSpace(value) ? Result.Success<TEnum?>(value: null) : EnumValueParser.ParseOptional<TEnum>(value, propertyName, errorMessage);
    }

    private static Result<ProductType?> ParseProductType(UpdateProductCommand command) {
        Result<ProductType?> parsedProductTypeResult = ParseOptionalEnum<ProductType>(
            command.ProductType,
            nameof(command.ProductType),
            "Unknown product type value.");
        if (parsedProductTypeResult.IsFailure) {
            return parsedProductTypeResult;
        }

        return parsedProductTypeResult.Value.HasValue && !Enum.IsDefined(parsedProductTypeResult.Value.Value)
            ? Result.Failure<ProductType?>(
                Errors.Validation.Invalid(nameof(command.ProductType), "Unknown product type value."))
            : parsedProductTypeResult;
    }

    private readonly record struct ProductUpdateImageValues(
        ImageAssetId? ImageAssetId,
        string? ImageUrl,
        bool HasResolvedImageAsset);
}
