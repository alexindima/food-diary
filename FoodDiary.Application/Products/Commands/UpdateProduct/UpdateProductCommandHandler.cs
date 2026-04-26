using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler(
    IProductRepository productRepository,
    IImageAssetCleanupService imageAssetCleanupService,
    IUserRepository userRepository)
    : ICommandHandler<UpdateProductCommand, Result<ProductModel>> {
    public async Task<Result<ProductModel>>
        Handle(UpdateProductCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ProductModel>(Errors.Authentication.InvalidToken);
        }

        if (command.ProductId == Guid.Empty) {
            return Result.Failure<ProductModel>(Errors.Validation.Invalid(nameof(command.ProductId), "Product id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<ProductModel>(accessError);
        }

        var productId = new ProductId(command.ProductId);

        var product = await productRepository.GetByIdAsync(
            productId,
            userId,
            includePublic: false,
            cancellationToken: cancellationToken);
        if (product is null) {
            return Result.Failure<ProductModel>(Errors.Product.NotAccessible(command.ProductId));
        }

        var modifiedOnBefore = product.ModifiedOnUtc;

        MeasurementUnit? newUnit = null;
        if (!string.IsNullOrWhiteSpace(command.BaseUnit)) {
            var parsedUnitResult = EnumValueParser.ParseOptional<MeasurementUnit>(
                command.BaseUnit,
                nameof(command.BaseUnit),
                "Unknown measurement unit value.");
            if (parsedUnitResult.IsFailure) {
                return Result.Failure<ProductModel>(parsedUnitResult.Error);
            }

            newUnit = parsedUnitResult.Value;
        }

        Visibility? newVisibility = null;
        if (!string.IsNullOrWhiteSpace(command.Visibility)) {
            var parsedVisibilityResult = EnumValueParser.ParseOptional<Visibility>(
                command.Visibility,
                nameof(command.Visibility),
                "Unknown visibility value.");
            if (parsedVisibilityResult.IsFailure) {
                return Result.Failure<ProductModel>(parsedVisibilityResult.Error);
            }

            newVisibility = parsedVisibilityResult.Value;
        }

        ProductType? newProductType = null;
        if (!string.IsNullOrWhiteSpace(command.ProductType) &&
            Enum.TryParse<ProductType>(command.ProductType, true, out var parsedProductType)) {
            newProductType = parsedProductType;
        }

        var imageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<ProductModel>(imageAssetIdResult.Error);
        }

        var oldAssetId = product.ImageAssetId;

        if (command.Name is not null ||
            command.Barcode is not null ||
            command.ClearBarcode ||
            command.Brand is not null ||
            command.ClearBrand ||
            newProductType.HasValue) {
            product.UpdateCoreIdentity(
                name: command.Name,
                barcode: command.Barcode,
                clearBarcode: command.ClearBarcode,
                brand: command.Brand,
                clearBrand: command.ClearBrand,
                productType: newProductType);
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

        if (newUnit.HasValue || command.BaseAmount.HasValue || command.DefaultPortionAmount.HasValue) {
            product.UpdateMeasurement(
                baseUnit: newUnit,
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

        if (command.ImageUrl is not null || command.ClearImageUrl || command.ImageAssetId.HasValue || command.ClearImageAssetId) {
            product.UpdateMedia(
                imageUrl: command.ImageUrl,
                clearImageUrl: command.ClearImageUrl,
                imageAssetId: imageAssetIdResult.Value,
                clearImageAssetId: command.ClearImageAssetId);
        }

        if (newVisibility.HasValue) {
            product.ChangeVisibility(newVisibility.Value);
        }

        var hasChanges = product.ModifiedOnUtc != modifiedOnBefore;
        if (hasChanges) {
            await productRepository.UpdateAsync(product, cancellationToken);
        }

        var imageAssetChanged = command.ClearImageAssetId ||
                                (command.ImageAssetId.HasValue && oldAssetId.HasValue && oldAssetId.Value.Value != command.ImageAssetId.Value);

        if (hasChanges && oldAssetId.HasValue && imageAssetChanged) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(oldAssetId.Value, cancellationToken);
        }

        var usageCount = product.MealItems.Count + product.RecipeIngredients.Count;
        return Result.Success(product.ToModel(usageCount, true));
    }
}
