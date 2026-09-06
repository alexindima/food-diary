using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class ProductExtractedInvariantTests {
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Product_Create_WithNonFiniteBaseAmount_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(
            UserId.New(), "Apple", MeasurementUnit.G, value, 100, 52, 0.3, 0.2, 14, 2.4, 0));
    }

    [Fact]
    public void Product_UpdateIdentity_WhenLateValidationFails_IsAtomic() {
        Product product = CreateProduct();

        Assert.Throws<ArgumentOutOfRangeException>(() => product.UpdateIdentity(new ProductIdentityUpdate(
            Name: "Changed",
            Description: new string('x', 2049))));

        Assert.Multiple(
            () => Assert.Equal("Product", product.Name),
            () => Assert.Null(product.ModifiedOnUtc));
    }

    [Fact]
    public void Product_EfNavigation_DefaultsToNull() {
        Product product = CreateProduct();

    }

    [Fact]
    public void ProductNutrition_Create_WithNegativeValue_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProductNutrition.Create(-1, 0, 0, 0, 0, 0));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ProductNutrition_Create_WithNonFiniteValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProductNutrition.Create(value, 0, 0, 0, 0, 0));
    }

    [Fact]
    public void ProductNutrition_IsCloseTo_RespectsEpsilon() {
        var left = ProductNutrition.Create(100, 10, 5, 20, 3, 0);
        var right = ProductNutrition.Create(100.0000005, 10, 5, 20, 3, 0);
        var far = ProductNutrition.Create(100.1, 10, 5, 20, 3, 0);

        Assert.True(left.IsCloseTo(right, 0.000001));
        Assert.False(left.IsCloseTo(far, 0.000001));
    }

    [Fact]
    public void ProductNutrition_With_UpdatesOnlyProvidedValues() {
        var original = ProductNutrition.Create(100, 10, 5, 20, 3, 0);

        ProductNutrition updated = original.With(caloriesPerBase: 200);

        Assert.Multiple(
            () => Assert.Equal(200, updated.CaloriesPerBase),
            () => Assert.Equal(10, updated.ProteinsPerBase),
            () => Assert.Equal(5, updated.FatsPerBase),
            () => Assert.Equal(20, updated.CarbsPerBase),
            () => Assert.Equal(3, updated.FiberPerBase),
            () => Assert.Equal(0, updated.AlcoholPerBase));
    }

    [Fact]
    public void ProductNutrition_With_WithNegativeValue_Throws() {
        var nutrition = ProductNutrition.Create(100, 10, 5, 20, 3, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => nutrition.With(fatsPerBase: -1));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ProductNutrition_With_WithNonFiniteValue_Throws(double value) {
        var nutrition = ProductNutrition.Create(100, 10, 5, 20, 3, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => nutrition.With(proteinsPerBase: value));
    }

    private static Product CreateProduct() => Product.Create(
        UserId.New(), "Product", MeasurementUnit.G, 100, defaultPortionAmount: null,
        caloriesPerBase: 100, proteinsPerBase: 10, fatsPerBase: 5, carbsPerBase: 10,
        fiberPerBase: 2, alcoholPerBase: 0);
}
