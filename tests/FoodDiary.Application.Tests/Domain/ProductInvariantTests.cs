using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Domain;

public class ProductInvariantTests
{
    private static Product CreateValidProduct()
    {
        return Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0);
    }

    [Fact]
    public void Create_WithInvalidName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Product.Create(
            UserId.New(),
            name: "   ",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 100,
            proteinsPerBase: 10,
            fatsPerBase: 10,
            carbsPerBase: 10,
            fiberPerBase: 1,
            alcoholPerBase: 0));
    }

    [Fact]
    public void Create_WithNegativeNutrition_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: -1,
            proteinsPerBase: 10,
            fatsPerBase: 10,
            carbsPerBase: 10,
            fiberPerBase: 1,
            alcoholPerBase: 0));
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => Product.Create(
            UserId.Empty,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0));
    }

    [Fact]
    public void Create_WithTooLongName_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(
            UserId.New(),
            name: new string('a', 257),
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0));
    }

    [Fact]
    public void Create_WithNonCanonicalBaseAmountForGram_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 50,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0));
    }

    [Fact]
    public void Create_WithWhitespaceOptionalFields_NormalizesToNull()
    {
        var product = Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            barcode: "   ",
            brand: "   ",
            category: "   ",
            description: "   ",
            comment: "   ",
            imageUrl: "   ");

        Assert.Null(product.Barcode);
        Assert.Null(product.Brand);
        Assert.Null(product.Category);
        Assert.Null(product.Description);
        Assert.Null(product.Comment);
        Assert.Null(product.ImageUrl);
    }

    [Fact]
    public void UpdateMeasurement_WithInvalidPortion_Throws()
    {
        var product = Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => product.UpdateMeasurement(defaultPortionAmount: 0));
    }

    [Fact]
    public void UpdateIdentity_WithClearBrand_ClearsBrand()
    {
        var product = Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            brand: "Brand");

        product.UpdateIdentity(clearBrand: true);

        Assert.Null(product.Brand);
    }

    [Fact]
    public void UpdateIdentity_WithPaddedBrand_NormalizesAndTrims()
    {
        var product = Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0);

        product.UpdateIdentity(brand: "  Brand  ");

        Assert.Equal("Brand", product.Brand);
    }

    [Fact]
    public void UpdateIdentity_WithTooLongBrand_Throws()
    {
        var product = Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            product.UpdateIdentity(brand: new string('b', 129)));
    }

    [Fact]
    public void UpdateIdentity_WithSameValues_DoesNotSetModifiedOnUtc()
    {
        var product = Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            brand: "Brand");

        product.UpdateIdentity(brand: " Brand ");

        Assert.Null(product.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateMedia_WithClearAndValue_Throws()
    {
        var product = Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0);

        Assert.Throws<ArgumentException>(() => product.UpdateMedia(imageUrl: "https://img", clearImageUrl: true));
    }

    [Fact]
    public void UpdateMeasurement_WithUnitChange_ResetsBaseAmountToCanonical()
    {
        var product = Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0);

        product.UpdateMeasurement(baseUnit: MeasurementUnit.PCS);

        Assert.Equal(MeasurementUnit.PCS, product.BaseUnit);
        Assert.Equal(1d, product.BaseAmount);
    }

    [Fact]
    public void UpdateMeasurement_WithNonCanonicalBaseAmountForPcs_Throws()
    {
        var product = CreateValidProduct();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            product.UpdateMeasurement(baseUnit: MeasurementUnit.PCS, baseAmount: 2));
    }

    [Fact]
    public void Create_WithNullDefaultPortionAmount_UsesBaseAmount()
    {
        var product = Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: null,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0);

        Assert.Equal(100d, product.DefaultPortionAmount);
    }

    [Fact]
    public void UpdateMeasurement_WithSameValues_DoesNotSetModifiedOnUtc()
    {
        var product = CreateValidProduct();

        product.UpdateMeasurement(baseAmount: 100, defaultPortionAmount: 100);

        Assert.Null(product.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateNutrition_WithNegativeValue_Throws()
    {
        var product = CreateValidProduct();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            product.UpdateNutrition(proteinsPerBase: -0.1));
    }

    [Fact]
    public void UpdateNutrition_WithPartialUpdate_PreservesOtherValues()
    {
        var product = CreateValidProduct();

        product.UpdateNutrition(proteinsPerBase: 1.5);

        Assert.Equal(52d, product.CaloriesPerBase);
        Assert.Equal(1.5, product.ProteinsPerBase);
        Assert.Equal(0.2, product.FatsPerBase);
        Assert.Equal(14d, product.CarbsPerBase);
        Assert.Equal(2.4, product.FiberPerBase);
        Assert.Equal(0d, product.AlcoholPerBase);
    }

    [Fact]
    public void UpdateNutrition_WithSameValues_DoesNotSetModifiedOnUtc()
    {
        var product = CreateValidProduct();

        product.UpdateNutrition(
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0);

        Assert.Null(product.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateMedia_WithClearImageAssetIdAndValue_Throws()
    {
        var product = CreateValidProduct();

        Assert.Throws<ArgumentException>(() =>
            product.UpdateMedia(imageAssetId: ImageAssetId.New(), clearImageAssetId: true));
    }

    [Fact]
    public void UpdateMedia_WithSameTrimmedImageUrl_DoesNotSetModifiedOnUtc()
    {
        var product = Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            imageUrl: "https://img");

        product.UpdateMedia(imageUrl: "  https://img  ");

        Assert.Null(product.ModifiedOnUtc);
    }

    [Fact]
    public void ChangeVisibility_WithSameValue_DoesNotSetModifiedOnUtc()
    {
        var product = CreateValidProduct();

        product.ChangeVisibility(Visibility.PUBLIC);

        Assert.Null(product.ModifiedOnUtc);
    }

    [Fact]
    public void ChangeVisibility_WithDifferentValue_UpdatesVisibility()
    {
        var product = CreateValidProduct();

        product.ChangeVisibility(Visibility.PRIVATE);

        Assert.Equal(Visibility.PRIVATE, product.Visibility);
        Assert.NotNull(product.ModifiedOnUtc);
    }
}

