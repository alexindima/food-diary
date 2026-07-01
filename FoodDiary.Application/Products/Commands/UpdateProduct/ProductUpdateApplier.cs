using FoodDiary.Domain.Entities.Products;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

internal static class ProductUpdateApplier {
    public static void Apply(Product product, UpdateProductCommand command, ProductUpdateValues values) {
        ApplyIdentityUpdates(product, command, values);
        ApplyMeasurementAndNutritionUpdates(product, command, values);
        ApplyMediaAndVisibilityUpdates(product, command, values);
    }

    private static void ApplyIdentityUpdates(
        Product product,
        UpdateProductCommand command,
        ProductUpdateValues values) {
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
        ProductUpdateValues values) {
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
        ProductUpdateValues values) {
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
}
