using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.ValueObjects;

namespace FoodDiary.Modules.Products.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProductMeasurementValueTests {
    [Theory]
    [InlineData(MeasurementUnit.G, 100)]
    [InlineData(MeasurementUnit.Ml, 100)]
    [InlineData(MeasurementUnit.Pcs, 1)]
    public void Basis_RetainsCanonicalUnitMeaning(MeasurementUnit unit, double amount) {
        var basis = ProductMeasurementBasis.FromFields(unit, amount);
        Assert.Multiple(() => Assert.Equal(unit, basis.Unit), () => Assert.Equal(amount, basis.Amount));
    }

    [Fact]
    public void Portions_KeepDifferentUnitLimitsAndFractionalPrecision() {
        Assert.Equal(10_000, ProductDefaultPortion.FromAmount(MeasurementUnit.G, 10_000).Amount);
        Assert.Equal(1000, ProductDefaultPortion.FromAmount(MeasurementUnit.Pcs, 1000).Amount);
        Assert.Equal(0.125, ProductDefaultPortion.FromAmount(MeasurementUnit.Pcs, 0.125).Amount);
        Assert.Throws<ArgumentOutOfRangeException>(() => ProductDefaultPortion.FromAmount(MeasurementUnit.Pcs, 1001));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProductMeasurementBasis.FromFields(MeasurementUnit.G, 1));
    }
}
