using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Domain.ValueObjects;
using FoodDiary.Modules.Products.Domain.Entities;

namespace FoodDiary.Modules.Products.Application.Commands.UpdateProduct;

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
            product.UpdateCoreIdentityChanges(new ProductCoreIdentityChanges(
                command.Name,
                FieldChanges.FromOptionalText(command.Barcode, command.ClearBarcode),
                FieldChanges.FromOptionalText(command.Brand, command.ClearBrand),
                values.ProductType));
        }

        if (command.Category is not null ||
            command.ClearCategory ||
            command.Description is not null ||
            command.ClearDescription ||
            command.Comment is not null ||
            command.ClearComment) {
            product.UpdateDescriptiveIdentityChanges(new ProductDescriptiveIdentityChanges(
                FieldChanges.FromOptionalText(command.Category, command.ClearCategory),
                FieldChanges.FromOptionalText(command.Description, command.ClearDescription),
                FieldChanges.FromOptionalText(command.Comment, command.ClearComment)));
        }
    }

    private static void ApplyMeasurementAndNutritionUpdates(
        Product product,
        UpdateProductCommand command,
        ProductUpdateValues values) {
        if (values.Unit.HasValue ||
            command.BaseAmount.HasValue ||
            command.DefaultPortionAmount.HasValue ||
            command.CaloriesPerBase.HasValue ||
            command.ProteinsPerBase.HasValue ||
            command.FatsPerBase.HasValue ||
            command.CarbsPerBase.HasValue ||
            command.FiberPerBase.HasValue ||
            command.AlcoholPerBase.HasValue) {
            FoodDiary.Modules.Products.Domain.Contracts.Enums.MeasurementUnit unit = values.Unit ?? product.BaseUnit;
            ProductMeasurementBasis? basis = null;
            if (command.BaseAmount is { } baseAmount) {
                basis = ProductMeasurementBasis.FromFields(unit, baseAmount);
            } else if (values.Unit.HasValue) {
                basis = ProductMeasurementBasis.Canonical(unit);
            }
            product.UpdateMeasurementNutritionChanges(new ProductMeasurementNutritionChanges(
                Basis: basis,
                DefaultPortion: command.DefaultPortionAmount is { } amount ? ProductDefaultPortion.FromAmount(unit, amount) : null,
                CaloriesPerBase: command.CaloriesPerBase,
                ProteinsPerBase: command.ProteinsPerBase,
                FatsPerBase: command.FatsPerBase,
                CarbsPerBase: command.CarbsPerBase,
                FiberPerBase: command.FiberPerBase,
                AlcoholPerBase: command.AlcoholPerBase));
        }
    }

    private static void ApplyMediaAndVisibilityUpdates(
        Product product,
        UpdateProductCommand command,
        ProductUpdateValues values) {
        if (command.ImageUrl is not null || command.ClearImageUrl || command.ImageAssetId.HasValue || command.ClearImageAssetId) {
            product.UpdateMediaChanges(new ProductMediaChanges(
                FieldChanges.FromOptionalText(values.ImageUrl, !values.HasResolvedImageAsset && command.ClearImageUrl),
                FieldChanges.FromOptionalValue(values.ImageAssetId, command.ClearImageAssetId)));
        }

        if (values.Visibility.HasValue) {
            product.ChangeVisibility(values.Visibility.Value);
        }
    }
}
