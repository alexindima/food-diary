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
    private sealed record UpdateProductValues(
        UserId UserId,
        ProductId ProductId,
        MeasurementUnit? Unit,
        Visibility? Visibility,
        ProductType? ProductType,
        ImageAssetId? ImageAssetId,
        string? ImageUrl,
        bool HasResolvedImageAsset);

    public async Task<Result<ProductModel>>
        Handle(UpdateProductCommand command, CancellationToken cancellationToken) {
        Result<UpdateProductValues> valuesResult = await PrepareUpdateValuesAsync(command, cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<ProductModel>(valuesResult.Error);
        }

        UpdateProductValues values = valuesResult.Value;
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
        ApplyProductUpdates(product, command, values);

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
        return Result.Success(product.ToModel(usageCount, true));
    }

    private async Task<Result<UpdateProductValues>> PrepareUpdateValuesAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await ResolveUserIdAsync(command, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure<UpdateProductValues>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        var productId = new ProductId(command.ProductId);
        Result<MeasurementUnit?> unitResult = ParseOptionalEnum<MeasurementUnit>(
            command.BaseUnit,
            nameof(command.BaseUnit),
            "Unknown measurement unit value.");
        if (unitResult.IsFailure) {
            return Result.Failure<UpdateProductValues>(unitResult.Error);
        }

        Result<Visibility?> visibilityResult = ParseOptionalEnum<Visibility>(
            command.Visibility,
            nameof(command.Visibility),
            "Unknown visibility value.");
        if (visibilityResult.IsFailure) {
            return Result.Failure<UpdateProductValues>(visibilityResult.Error);
        }

        Result<ProductType?> productTypeResult = ParseProductType(command);
        if (productTypeResult.IsFailure) {
            return Result.Failure<UpdateProductValues>(productTypeResult.Error);
        }

        Result<(ImageAssetId? ImageAssetId, string? ImageUrl, bool HasResolvedImageAsset)> imageAssetResult = await ResolveImageAssetAsync(command, userId, cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<UpdateProductValues>(imageAssetResult.Error);
        }

        return Result.Success(new UpdateProductValues(
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
        if (string.IsNullOrWhiteSpace(value)) {
            return Result.Success<TEnum?>(null);
        }

        return EnumValueParser.ParseOptional<TEnum>(value, propertyName, errorMessage);
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

    private static void ApplyProductUpdates(
        Product product,
        UpdateProductCommand command,
        UpdateProductValues values) {
        ApplyIdentityUpdates(product, command, values);
        ApplyMeasurementAndNutritionUpdates(product, command, values);
        ApplyMediaAndVisibilityUpdates(product, command, values);
    }

    private static void ApplyIdentityUpdates(
        Product product,
        UpdateProductCommand command,
        UpdateProductValues values) {
        if (command.Name is not null ||
            command.Barcode is not null ||
            command.ClearBarcode ||
            command.Brand is not null ||
            command.ClearBrand ||
            values.ProductType.HasValue) {
            product.UpdateCoreIdentity(
                name: command.Name,
                barcode: command.Barcode,
                clearBarcode: command.ClearBarcode,
                brand: command.Brand,
                clearBrand: command.ClearBrand,
                productType: values.ProductType);
        }

        if (command.Category is not null ||
            command.ClearCategory ||
            command.Description is not null ||
            command.ClearDescription ||
            command.Comment is not null ||
            command.ClearComment) {
            product.UpdateDescriptiveIdentity(
                category: command.Category,
                clearCategory: command.ClearCategory,
                description: command.Description,
                clearDescription: command.ClearDescription,
                comment: command.Comment,
                clearComment: command.ClearComment);
        }
    }

    private static void ApplyMeasurementAndNutritionUpdates(
        Product product,
        UpdateProductCommand command,
        UpdateProductValues values) {
        if (values.Unit.HasValue || command.BaseAmount.HasValue || command.DefaultPortionAmount.HasValue) {
            product.UpdateMeasurement(
                baseUnit: values.Unit,
                baseAmount: command.BaseAmount,
                defaultPortionAmount: command.DefaultPortionAmount);
        }

        if (command.CaloriesPerBase.HasValue ||
            command.ProteinsPerBase.HasValue ||
            command.FatsPerBase.HasValue ||
            command.CarbsPerBase.HasValue ||
            command.FiberPerBase.HasValue ||
            command.AlcoholPerBase.HasValue) {
            product.UpdateNutrition(
                caloriesPerBase: command.CaloriesPerBase,
                proteinsPerBase: command.ProteinsPerBase,
                fatsPerBase: command.FatsPerBase,
                carbsPerBase: command.CarbsPerBase,
                fiberPerBase: command.FiberPerBase,
                alcoholPerBase: command.AlcoholPerBase);
        }
    }

    private static void ApplyMediaAndVisibilityUpdates(
        Product product,
        UpdateProductCommand command,
        UpdateProductValues values) {
        if (command.ImageUrl is not null || command.ClearImageUrl || command.ImageAssetId.HasValue || command.ClearImageAssetId) {
            product.UpdateMedia(
                imageUrl: values.ImageUrl,
                clearImageUrl: !values.HasResolvedImageAsset && command.ClearImageUrl,
                imageAssetId: values.ImageAssetId,
                clearImageAssetId: command.ClearImageAssetId);
        }

        if (values.Visibility.HasValue) {
            product.ChangeVisibility(values.Visibility.Value);
        }
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
