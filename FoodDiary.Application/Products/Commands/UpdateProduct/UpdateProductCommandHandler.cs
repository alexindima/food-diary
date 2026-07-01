using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Assets;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler(
    IProductRepository productRepository,
    IImageAssetCleanupService imageAssetCleanupService,
    IUserRepository userRepository,
    IImageAssetAccessService imageAssetAccessService)
    : ICommandHandler<UpdateProductCommand, Result<ProductModel>> {
    public async Task<Result<ProductModel>>
        Handle(UpdateProductCommand command, CancellationToken cancellationToken) {
        Result<ProductUpdateValues> valuesResult = await PrepareUpdateValuesAsync(command, cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<ProductModel>(valuesResult.Error);
        }

        ProductUpdateValues values = valuesResult.Value;
        Product? product = await productRepository.GetByIdForUpdateAsync(
            values.ProductId,
            values.UserId,
            includePublic: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (product is null) {
            return Result.Failure<ProductModel>(Errors.Product.NotAccessible(command.ProductId));
        }

        ImageAssetId? oldAssetId = product.ImageAssetId;
        DateTime? modifiedOnBefore = product.ModifiedOnUtc;
        ProductUpdateApplier.Apply(product, command, values);

        bool hasChanges = product.ModifiedOnUtc != modifiedOnBefore;
        if (hasChanges) {
            await productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        }

        await CleanupOldImageAssetAsync(
            oldAssetId,
            command,
            hasChanges,
            cancellationToken).ConfigureAwait(false);

        int usageCount = product.MealItems.Count + product.RecipeIngredients.Count;
        return Result.Success(product.ToModel(usageCount, isOwnedByCurrentUser: true));
    }

    private async Task<Result<ProductUpdateValues>> PrepareUpdateValuesAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await ResolveUserIdAsync(command, cancellationToken).ConfigureAwait(false);
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

        Result<(ImageAssetId? ImageAssetId, string? ImageUrl, bool HasResolvedImageAsset)> imageAssetResult = await ResolveImageAssetAsync(command, userId, cancellationToken).ConfigureAwait(false);
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

    private async Task<Result<UserId>> ResolveUserIdAsync(
        UpdateProductCommand command,
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

    private async Task<Result<(ImageAssetId? ImageAssetId, string? ImageUrl, bool HasResolvedImageAsset)>> ResolveImageAssetAsync(
        UpdateProductCommand command,
        UserId userId,
        CancellationToken cancellationToken) {
        Result<ImageAssetId?> imageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<(ImageAssetId? ImageAssetId, string? ImageUrl, bool HasResolvedImageAsset)>(imageAssetIdResult.Error);
        }

        Result<ImageAsset?> imageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            imageAssetIdResult.Value,
            userId,
            cancellationToken).ConfigureAwait(false);
        return imageAssetResult.IsFailure
            ? Result.Failure<(ImageAssetId? ImageAssetId, string? ImageUrl, bool HasResolvedImageAsset)>(imageAssetResult.Error)
            : Result.Success((imageAssetIdResult.Value, imageAssetResult.Value?.Url ?? command.ImageUrl, imageAssetResult.Value is not null));
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

    private async Task CleanupOldImageAssetAsync(
        ImageAssetId? oldAssetId,
        UpdateProductCommand command,
        bool hasChanges,
        CancellationToken cancellationToken) {
        bool imageAssetChanged = command.ClearImageAssetId ||
                                (command.ImageAssetId.HasValue && (!oldAssetId.HasValue || oldAssetId.Value.Value != command.ImageAssetId.Value));

        if (hasChanges && oldAssetId.HasValue && imageAssetChanged) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(oldAssetId.Value, cancellationToken).ConfigureAwait(false);
        }
    }
}
