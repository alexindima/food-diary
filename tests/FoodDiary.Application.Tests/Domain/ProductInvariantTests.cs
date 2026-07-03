using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public class ProductInvariantTests {
    private static Product CreateValidProduct() {
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
    public void Create_WithInvalidName_Throws() {
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
    public void Create_WithNegativeNutrition_Throws() {
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
    public void Create_WithEmptyUserId_Throws() {
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
    public void Create_WithTooLongName_Throws() {
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
    public void Create_WithNonCanonicalBaseAmountForGram_Throws() {
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
    public void Create_WithWhitespaceOptionalFields_NormalizesToNull() {
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

        Assert.Multiple(
            () => Assert.Null(product.Barcode),
            () => Assert.Null(product.Brand),
            () => Assert.Null(product.Category),
            () => Assert.Null(product.Description),
            () => Assert.Null(product.Comment),
            () => Assert.Null(product.ImageUrl));
    }

    [Fact]
    public void Create_WithAllOptionalFields_NormalizesAndStoresThem() {
        var imageAssetId = ImageAssetId.New();

        var product = Product.Create(
            UserId.New(),
            name: "  Yogurt  ",
            baseUnit: MeasurementUnit.Ml,
            baseAmount: 100,
            defaultPortionAmount: 125,
            caloriesPerBase: 64,
            proteinsPerBase: 3,
            fatsPerBase: 2,
            carbsPerBase: 8,
            fiberPerBase: 1,
            alcoholPerBase: 0,
            barcode: "  123456789  ",
            brand: "  Dairy Co  ",
            productType: ProductType.Dairy,
            category: "  Dairy  ",
            description: "  Plain yogurt  ",
            comment: "  Breakfast  ",
            imageUrl: "  https://img.example/yogurt.jpg  ",
            imageAssetId,
            visibility: Visibility.Private);

        Assert.Multiple(
            () => Assert.Equal("Yogurt", product.Name),
            () => Assert.Equal("123456789", product.Barcode),
            () => Assert.Equal("Dairy Co", product.Brand),
            () => Assert.Equal(ProductType.Dairy, product.ProductType),
            () => Assert.Equal("Dairy", product.Category),
            () => Assert.Equal("Plain yogurt", product.Description),
            () => Assert.Equal("Breakfast", product.Comment),
            () => Assert.Equal("https://img.example/yogurt.jpg", product.ImageUrl),
            () => Assert.Equal(imageAssetId, product.ImageAssetId),
            () => Assert.Equal(Visibility.Private, product.Visibility),
            () => Assert.Equal(125d, product.DefaultPortionAmount));
    }

    [Fact]
    public void Create_WithInvalidAmounts_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(
            UserId.New(),
            "Apple",
            MeasurementUnit.G,
            baseAmount: 0,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(
            UserId.New(),
            "Apple",
            MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 0,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0));
    }

    [Theory]
    [InlineData(MeasurementUnit.G, Product.MaxWeightOrVolumeDefaultPortionAmount + 1)]
    [InlineData(MeasurementUnit.Ml, Product.MaxWeightOrVolumeDefaultPortionAmount + 1)]
    [InlineData(MeasurementUnit.Pcs, Product.MaxPieceDefaultPortionAmount + 1)]
    public void Create_WithDefaultPortionAmountAboveUnitLimit_Throws(MeasurementUnit unit, double defaultPortionAmount) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(
            UserId.New(),
            "Apple",
            unit,
            baseAmount: unit == MeasurementUnit.Pcs ? 1 : 100,
            defaultPortionAmount: defaultPortionAmount,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Create_WithNonFiniteNutrition_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(
            UserId.New(),
            "Apple",
            MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: value,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0));
    }

    [Theory]
    [InlineData(MeasurementUnit.G, Product.MaxWeightOrVolumeCaloriesPerBase + 1)]
    [InlineData(MeasurementUnit.Ml, Product.MaxWeightOrVolumeCaloriesPerBase + 1)]
    [InlineData(MeasurementUnit.Pcs, Product.MaxPieceCaloriesPerBase + 1)]
    public void Create_WithCaloriesAboveUnitLimit_Throws(MeasurementUnit unit, double caloriesPerBase) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(
            UserId.New(),
            "Apple",
            unit,
            baseAmount: unit == MeasurementUnit.Pcs ? 1 : 100,
            defaultPortionAmount: unit == MeasurementUnit.Pcs ? 1 : 100,
            caloriesPerBase: caloriesPerBase,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0));
    }

    [Theory]
    [InlineData(MeasurementUnit.G, Product.MaxWeightOrVolumeNutrientPerBase + 1)]
    [InlineData(MeasurementUnit.Ml, Product.MaxWeightOrVolumeNutrientPerBase + 1)]
    [InlineData(MeasurementUnit.Pcs, Product.MaxPieceNutrientPerBase + 1)]
    public void Create_WithNutrientAboveUnitLimit_Throws(MeasurementUnit unit, double nutrientPerBase) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(
            UserId.New(),
            "Apple",
            unit,
            baseAmount: unit == MeasurementUnit.Pcs ? 1 : 100,
            defaultPortionAmount: unit == MeasurementUnit.Pcs ? 1 : 100,
            caloriesPerBase: 52,
            proteinsPerBase: nutrientPerBase,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0));
    }

    [Fact]
    public void Create_WithTooLongOptionalFields_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(UserId.New(), "Apple", MeasurementUnit.G, 100, 100, 52, 0.3, 0.2, 14, 2.4, 0, barcode: new string('b', 129)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(UserId.New(), "Apple", MeasurementUnit.G, 100, 100, 52, 0.3, 0.2, 14, 2.4, 0, category: new string('c', 129)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(UserId.New(), "Apple", MeasurementUnit.G, 100, 100, 52, 0.3, 0.2, 14, 2.4, 0, description: new string('d', 2049)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(UserId.New(), "Apple", MeasurementUnit.G, 100, 100, 52, 0.3, 0.2, 14, 2.4, 0, comment: new string('c', 2049)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(UserId.New(), "Apple", MeasurementUnit.G, 100, 100, 52, 0.3, 0.2, 14, 2.4, 0, imageUrl: new string('i', 2049)));
    }

    [Fact]
    public void UpdateMeasurement_WithInvalidPortion_Throws() {
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

    [Theory]
    [InlineData(MeasurementUnit.G, Product.MaxWeightOrVolumeDefaultPortionAmount + 1)]
    [InlineData(MeasurementUnit.Ml, Product.MaxWeightOrVolumeDefaultPortionAmount + 1)]
    [InlineData(MeasurementUnit.Pcs, Product.MaxPieceDefaultPortionAmount + 1)]
    public void UpdateMeasurement_WithDefaultPortionAmountAboveUnitLimit_Throws(MeasurementUnit unit, double defaultPortionAmount) {
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

        Assert.Throws<ArgumentOutOfRangeException>(() => product.UpdateMeasurement(
            baseUnit: unit,
            baseAmount: unit == MeasurementUnit.Pcs ? 1 : 100,
            defaultPortionAmount: defaultPortionAmount));
    }

    [Fact]
    public void UpdateIdentity_WithClearBrand_ClearsBrand() {
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

        product.UpdateIdentity(new ProductIdentityUpdate(ClearBrand: true));

        Assert.Null(product.Brand);
    }

    [Fact]
    public void UpdateIdentity_WithPaddedBrand_NormalizesAndTrims() {
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

        product.UpdateIdentity(new ProductIdentityUpdate(Brand: "  Brand  "));

        Assert.Equal("Brand", product.Brand);
    }

    [Fact]
    public void UpdateCoreIdentity_WithClears_RemovesOptionalFields() {
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
            barcode: "123",
            brand: "Brand");

        product.UpdateCoreIdentity(clearBarcode: true, clearBrand: true, productType: ProductType.Fruit);

        Assert.Multiple(
            () => Assert.Null(product.Barcode),
            () => Assert.Null(product.Brand),
            () => Assert.Equal(ProductType.Fruit, product.ProductType));
        Assert.NotNull(product.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateCoreIdentity_WithClearConflicts_Throws() {
        Product product = CreateValidProduct();

        Assert.Throws<ArgumentException>(() => product.UpdateCoreIdentity(barcode: "123", clearBarcode: true));
        Assert.Throws<ArgumentException>(() => product.UpdateCoreIdentity(brand: "Brand", clearBrand: true));
    }

    [Fact]
    public void UpdateIdentity_WithTooLongBrand_Throws() {
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
            product.UpdateIdentity(new ProductIdentityUpdate(Brand: new string('b', 129))));
    }

    [Fact]
    public void UpdateIdentity_WithSameValues_DoesNotSetModifiedOnUtc() {
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

        product.UpdateIdentity(new ProductIdentityUpdate(Brand: " Brand "));

        Assert.Null(product.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateCoreIdentity_WithPaddedNameAndBrand_NormalizesValues() {
        Product product = CreateValidProduct();

        product.UpdateCoreIdentity(name: "  Green Apple  ", brand: "  Farm  ");

        Assert.Equal("Green Apple", product.Name);
        Assert.Equal("Farm", product.Brand);
    }

    [Fact]
    public void UpdateCoreIdentity_WithBarcode_NormalizesBarcode() {
        Product product = CreateValidProduct();

        product.UpdateCoreIdentity(barcode: "  123456  ");

        Assert.Equal("123456", product.Barcode);
        Assert.NotNull(product.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateDescriptiveIdentity_WithPartialUpdate_PreservesOtherFields() {
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
            category: "Fruit",
            description: "Fresh");

        product.UpdateDescriptiveIdentity(comment: " seasonal ");

        Assert.Multiple(
            () => Assert.Equal("Fruit", product.Category),
            () => Assert.Equal("Fresh", product.Description),
            () => Assert.Equal("seasonal", product.Comment));
    }

    [Fact]
    public void UpdateDescriptiveIdentity_WithCategoryAndDescription_NormalizesValues() {
        Product product = CreateValidProduct();

        product.UpdateDescriptiveIdentity(category: "  Fruit  ", description: "  Fresh apple  ");

        Assert.Equal("Fruit", product.Category);
        Assert.Equal("Fresh apple", product.Description);
        Assert.NotNull(product.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateDescriptiveIdentity_WithClears_RemovesOptionalFields() {
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
            category: "Fruit",
            description: "Fresh",
            comment: "Seasonal");

        product.UpdateDescriptiveIdentity(clearCategory: true, clearDescription: true, clearComment: true);

        Assert.Multiple(
            () => Assert.Null(product.Category),
            () => Assert.Null(product.Description),
            () => Assert.Null(product.Comment));
        Assert.NotNull(product.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateDescriptiveIdentity_WithClearConflicts_Throws() {
        Product product = CreateValidProduct();

        Assert.Throws<ArgumentException>(() => product.UpdateDescriptiveIdentity(category: "Fruit", clearCategory: true));
        Assert.Throws<ArgumentException>(() => product.UpdateDescriptiveIdentity(description: "Fresh", clearDescription: true));
        Assert.Throws<ArgumentException>(() => product.UpdateDescriptiveIdentity(comment: "Note", clearComment: true));
    }

    [Fact]
    public void UpdateMedia_WithClearAndValue_Throws() {
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
    public void UpdateMedia_WithNewValuesAndClears_UpdatesMediaState() {
        var imageAssetId = ImageAssetId.New();
        Product product = CreateValidProduct();

        product.UpdateMedia(imageUrl: "  https://img.example/apple.jpg  ", imageAssetId: imageAssetId);

        Assert.Equal("https://img.example/apple.jpg", product.ImageUrl);
        Assert.Equal(imageAssetId, product.ImageAssetId);
        Assert.NotNull(product.ModifiedOnUtc);

        product.UpdateMedia(clearImageUrl: true, clearImageAssetId: true);

        Assert.Null(product.ImageUrl);
        Assert.Null(product.ImageAssetId);
    }

    [Fact]
    public void UpdateMeasurement_WithUnitChange_ResetsBaseAmountToCanonical() {
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

        product.UpdateMeasurement(baseUnit: MeasurementUnit.Pcs);

        Assert.Equal(MeasurementUnit.Pcs, product.BaseUnit);
        Assert.Equal(1d, product.BaseAmount);
    }

    [Fact]
    public void UpdateMeasurement_WithNonCanonicalBaseAmountForPcs_Throws() {
        Product product = CreateValidProduct();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            product.UpdateMeasurement(baseUnit: MeasurementUnit.Pcs, baseAmount: 2));
    }

    [Fact]
    public void UpdateMeasurement_ToStricterUnitWithExistingNutritionAboveLimit_Throws() {
        var product = Product.Create(
            UserId.New(),
            name: "Large piece",
            baseUnit: MeasurementUnit.Pcs,
            baseAmount: 1,
            defaultPortionAmount: 1,
            caloriesPerBase: Product.MaxWeightOrVolumeCaloriesPerBase + 1,
            proteinsPerBase: 0,
            fatsPerBase: 0,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            product.UpdateMeasurement(baseUnit: MeasurementUnit.G, baseAmount: 100, defaultPortionAmount: 100));
    }

    [Fact]
    public void Create_WithNullDefaultPortionAmount_UsesBaseAmount() {
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
    public void UpdateMeasurement_WithSameValues_DoesNotSetModifiedOnUtc() {
        Product product = CreateValidProduct();

        product.UpdateMeasurement(baseAmount: 100, defaultPortionAmount: 100);

        Assert.Null(product.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateMeasurement_WithDifferentDefaultPortion_SetsModifiedOnUtc() {
        Product product = CreateValidProduct();

        product.UpdateMeasurement(defaultPortionAmount: 150);

        Assert.Equal(150d, product.DefaultPortionAmount);
        Assert.NotNull(product.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateNutrition_WithNegativeValue_Throws() {
        Product product = CreateValidProduct();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            product.UpdateNutrition(proteinsPerBase: -0.1));
    }

    [Fact]
    public void UpdateNutrition_WithPartialUpdate_PreservesOtherValues() {
        Product product = CreateValidProduct();

        product.UpdateNutrition(proteinsPerBase: 1.5);

        Assert.Multiple(
            () => Assert.Equal(52d, product.CaloriesPerBase),
            () => Assert.Equal(1.5, product.ProteinsPerBase),
            () => Assert.Equal(0.2, product.FatsPerBase),
            () => Assert.Equal(14d, product.CarbsPerBase),
            () => Assert.Equal(2.4, product.FiberPerBase),
            () => Assert.Equal(0d, product.AlcoholPerBase));
    }

    [Fact]
    public void UpdateNutrition_WithSameValues_DoesNotSetModifiedOnUtc() {
        Product product = CreateValidProduct();

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
    public void UpdateNutrition_WithNonFiniteValue_Throws() {
        Product product = CreateValidProduct();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            product.UpdateNutrition(caloriesPerBase: double.PositiveInfinity));
    }

    [Fact]
    public void UpdateNutrition_WithCaloriesAboveUnitLimit_Throws() {
        Product product = CreateValidProduct();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            product.UpdateNutrition(caloriesPerBase: Product.MaxWeightOrVolumeCaloriesPerBase + 1));
    }

    [Fact]
    public void UpdateNutrition_WithNutrientAboveUnitLimit_Throws() {
        Product product = CreateValidProduct();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            product.UpdateNutrition(proteinsPerBase: Product.MaxWeightOrVolumeNutrientPerBase + 1));
    }

    [Fact]
    public void LinkAndUnlinkUsdaFood_UpdateOnlyWhenValueChanges() {
        Product product = CreateValidProduct();

        product.LinkToUsdaFood(123);

        Assert.Equal(123, product.UsdaFdcId);
        Assert.NotNull(product.ModifiedOnUtc);

        product.LinkToUsdaFood(123);
        product.UnlinkUsdaFood();

        Assert.Null(product.UsdaFdcId);
    }

    [Fact]
    public void LinkToUsdaFood_WithInvalidFdcId_Throws() {
        Product product = CreateValidProduct();

        Assert.Throws<ArgumentOutOfRangeException>(() => product.LinkToUsdaFood(0));
    }

    [Fact]
    public void UnlinkUsdaFood_WhenNotLinked_DoesNotSetModifiedOnUtc() {
        Product product = CreateValidProduct();

        product.UnlinkUsdaFood();

        Assert.Null(product.ModifiedOnUtc);
    }

    [Fact]
    public void GetQualityScore_ReturnsScoreForProductNutrition() {
        Product product = CreateValidProduct();

        FoodQualityScore score = product.GetQualityScore();

        Assert.InRange(score.Score, 0, 100);
    }

    [Fact]
    public void UpdateMedia_WithClearImageAssetIdAndValue_Throws() {
        Product product = CreateValidProduct();

        Assert.Throws<ArgumentException>(() =>
            product.UpdateMedia(imageAssetId: ImageAssetId.New(), clearImageAssetId: true));
    }

    [Fact]
    public void UpdateMedia_WithSameTrimmedImageUrl_DoesNotSetModifiedOnUtc() {
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
    public void ChangeVisibility_WithSameValue_DoesNotSetModifiedOnUtc() {
        Product product = CreateValidProduct();

        product.ChangeVisibility(Visibility.Public);

        Assert.Null(product.ModifiedOnUtc);
    }

    [Fact]
    public void ChangeVisibility_WithDifferentValue_UpdatesVisibility() {
        Product product = CreateValidProduct();

        product.ChangeVisibility(Visibility.Private);

        Assert.Equal(Visibility.Private, product.Visibility);
        Assert.NotNull(product.ModifiedOnUtc);
    }

    [Fact]
    public void NavigationCollections_AreExposedAsReadOnly() {
        Product product = CreateValidProduct();

        ICollection<MealItem> mealItems = Assert.IsAssignableFrom<ICollection<FoodDiary.Domain.Entities.Meals.MealItem>>(product.MealItems);
        ICollection<RecipeIngredient> recipeIngredients = Assert.IsAssignableFrom<ICollection<FoodDiary.Domain.Entities.Recipes.RecipeIngredient>>(product.RecipeIngredients);

        Assert.True(mealItems.IsReadOnly);
        Assert.True(recipeIngredients.IsReadOnly);
    }
}
