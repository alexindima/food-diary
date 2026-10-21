using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Products.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class CopiedProductUpdateTests {
    [Fact]
    public void IdentityCopies_SetAndClearIndependentFieldsWithoutChangingTheTemplate() {
        Product product = CreateProduct();
        ProductIdentityUpdate template = new();
        product.UpdateIdentity(template with {
            Name = " Oats ",
            Barcode = " 12345 ",
            Brand = " Brand ",
            Category = " Grain ",
            ProductType = ProductType.Grain,
            Description = " Description ",
            Comment = " Comment ",
        });
        Assert.Multiple(
            () => Assert.Equal("Oats", product.Name),
            () => Assert.Equal("12345", product.Barcode),
            () => Assert.Equal("Brand", product.Brand),
            () => Assert.Equal("Grain", product.Category),
            () => Assert.Equal(ProductType.Grain, product.ProductType),
            () => Assert.Equal("Description", product.Description),
            () => Assert.Equal("Comment", product.Comment));

        product.UpdateIdentity(template with {
            ClearBarcode = true,
            ClearBrand = true,
            ClearCategory = true,
            ClearDescription = true,
            ClearComment = true,
        });

        Assert.Multiple(
            () => Assert.Null(product.Barcode),
            () => Assert.Null(product.Brand),
            () => Assert.Null(product.Category),
            () => Assert.Null(product.Description),
            () => Assert.Null(product.Comment),
            () => Assert.Equal("Oats", product.Name),
            () => Assert.Equal(ProductType.Grain, product.ProductType),
            () => Assert.Equal(default, template));
    }

    [Fact]
    public void MeasurementCopy_PreservesTheDeclaredBasisAndEveryNutrientWithoutImplicitScaling() {
        Product product = CreateProduct();
        ProductMeasurementNutritionUpdate template = new();

        product.UpdateMeasurementAndNutrition(template with {
            BaseUnit = MeasurementUnit.Ml,
            BaseAmount = 100,
            DefaultPortionAmount = 250,
            CaloriesPerBase = 102.25,
            ProteinsPerBase = 3.5,
            FatsPerBase = 4.25,
            CarbsPerBase = 12.75,
            FiberPerBase = 2.5,
            AlcoholPerBase = 1.25,
        });

        Assert.Multiple(
            () => Assert.Equal(MeasurementUnit.Ml, product.BaseUnit),
            () => Assert.Equal(100, product.BaseAmount),
            () => Assert.Equal(250, product.DefaultPortionAmount),
            () => Assert.Equal(102.25, product.CaloriesPerBase),
            () => Assert.Equal(3.5, product.ProteinsPerBase),
            () => Assert.Equal(4.25, product.FatsPerBase),
            () => Assert.Equal(12.75, product.CarbsPerBase),
            () => Assert.Equal(2.5, product.FiberPerBase),
            () => Assert.Equal(1.25, product.AlcoholPerBase),
            () => Assert.Equal(default, template));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TypedCreation_RejectsMissingBasisAndMismatchedPortionUnits(bool missingBasis) {
        var user = UserId.New();
        if (missingBasis) {
            Assert.Throws<ArgumentNullException>("basis", () => Product.CreateWithMeasurements(user, "Food", null!,
                defaultPortion: null, 100, 1, 2, 3, 4, 0));
        } else {
            var basis = ProductMeasurementBasis.Canonical(MeasurementUnit.G);
            var portion = ProductDefaultPortion.FromAmount(MeasurementUnit.Ml, 100);
            Assert.Throws<ArgumentException>("defaultPortion", () => Product.CreateWithMeasurements(user, "Food", basis,
                portion, 100, 1, 2, 3, 4, 0));
        }
    }

    [Fact]
    public void TypedUpdate_RejectsAContradictoryPortionBeforeChangingAnyStoredValues() {
        Product product = CreateProduct();

        Assert.Throws<ArgumentException>("changes", () => product.UpdateMeasurementNutritionChanges(
            new ProductMeasurementNutritionChanges(DefaultPortion: ProductDefaultPortion.FromAmount(MeasurementUnit.Ml, 100), CaloriesPerBase: 999)));

        Assert.Multiple(
            () => Assert.Equal(MeasurementUnit.G, product.BaseUnit),
            () => Assert.Equal(100, product.BaseAmount),
            () => Assert.Equal(100, product.CaloriesPerBase),
            () => Assert.Null(product.ModifiedOnUtc));
    }

    private static Product CreateProduct() => Product.Create(UserId.New(), "Food", MeasurementUnit.G, 100,
        defaultPortionAmount: null, 100, 1, 2, 3, 4, 0);
}
