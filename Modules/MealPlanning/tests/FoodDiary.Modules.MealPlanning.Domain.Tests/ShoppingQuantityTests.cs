using FoodDiary.Modules.MealPlanning.Domain.ValueObjects;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;

namespace FoodDiary.Modules.MealPlanning.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ShoppingQuantityTests {
    [Theory]
    [InlineData(null, null)]
    [InlineData(12.5, null)]
    [InlineData(null, MeasurementUnit.G)]
    [InlineData(12.5, MeasurementUnit.Ml)]
    public void ShoppingQuantity_RetainsPartialInputs(double? amount, MeasurementUnit? unit) {
        var quantity = ShoppingQuantity.FromFields(amount, unit);
        Assert.Multiple(() => Assert.Equal(amount, quantity.Amount), () => Assert.Equal(unit, quantity.Unit));
    }

    [Fact]
    public void SourceObservation_UsesItsOwnUnboundedContract() {
        const MeasurementUnit legacyUnit = (MeasurementUnit)987;
        var source = ShoppingSourceQuantity.FromFields(2_000_000, legacyUnit);
        Assert.Multiple(() => Assert.Equal(2_000_000, source.Amount), () => Assert.Equal(legacyUnit, source.Unit));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShoppingQuantity.FromFields(2_000_000, unit: null));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShoppingQuantity.FromFields(amount: null, legacyUnit));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShoppingSourceQuantity.FromFields(double.PositiveInfinity, unit: null));
    }

}
