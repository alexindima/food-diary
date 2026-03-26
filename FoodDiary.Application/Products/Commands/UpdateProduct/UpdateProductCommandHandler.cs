using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler(
    IProductRepository productRepository,
    IImageAssetCleanupService imageAssetCleanupService)
    : ICommandHandler<UpdateProductCommand, Result<ProductModel>> {
    public async Task<Result<ProductModel>>
        Handle(UpdateProductCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ProductModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
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
            if (!Enum.TryParse<MeasurementUnit>(command.BaseUnit, true, out var parsedUnit)) {
                return Result.Failure<ProductModel>(
                    Errors.Validation.Invalid(nameof(command.BaseUnit), "Unknown measurement unit value."));
            }

            newUnit = parsedUnit;
        }

        Visibility? newVisibility = null;
        if (!string.IsNullOrWhiteSpace(command.Visibility)) {
            if (!Enum.TryParse<Visibility>(command.Visibility, true, out var parsedVisibility)) {
                return Result.Failure<ProductModel>(
                    Errors.Validation.Invalid(nameof(command.Visibility), "Unknown visibility value."));
            }

            newVisibility = parsedVisibility;
        }

        ProductType? newProductType = null;
        if (!string.IsNullOrWhiteSpace(command.ProductType) &&
            Enum.TryParse<ProductType>(command.ProductType, true, out var parsedProductType)) {
            newProductType = parsedProductType;
        }

        var oldAssetId = product.ImageAssetId;

        if (command.Name is not null ||
            command.Barcode is not null ||
            command.ClearBarcode ||
            command.Brand is not null ||
            command.ClearBrand ||
            newProductType.HasValue ||
            command.Category is not null ||
            command.ClearCategory ||
            command.Description is not null ||
            command.ClearDescription ||
            command.Comment is not null ||
            command.ClearComment) {
            product.UpdateIdentity(
                name: command.Name,
                barcode: command.Barcode,
                clearBarcode: command.ClearBarcode,
                brand: command.Brand,
                clearBrand: command.ClearBrand,
                productType: newProductType,
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
                imageAssetId: command.ImageAssetId.HasValue ? new ImageAssetId(command.ImageAssetId.Value) : null,
                clearImageAssetId: command.ClearImageAssetId);
        }

        if (newVisibility.HasValue) {
            product.ChangeVisibility(newVisibility.Value);
        }

        var hasChanges = product.ModifiedOnUtc != modifiedOnBefore;
        if (hasChanges) {
            await productRepository.UpdateAsync(product);
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
