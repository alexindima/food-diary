using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Products.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProductUnitQuantityTests {
    [Theory]
    [InlineData(0.000000001)]
    [InlineData(0.125)]
    [InlineData(123.456789)]
    [InlineData(1000000)]
    public void FromUnits_PreservesFractionalPrecision(double amount) {
        var quantity = ProductUnitQuantity.FromUnits(amount);
        Assert.Equal(amount, quantity.Value);
        Assert.Equal(quantity, ProductUnitQuantity.FromUnits(amount));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000000.000001)]
    [InlineData(double.MaxValue)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void FromUnits_RejectsInvalidMutationQuantities(double amount) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => ProductUnitQuantity.FromUnits(amount));
        Assert.Equal("amount", error.ParamName);
    }
}
